using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.SignalR.Connections;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orleans.SignalR.Clients
{
    [StorageProvider(ProviderName = SignalrConstants.STORAGE_PROVIDER)]
    [Reentrant]
    internal class ClientGrain : Grain<ClientState>, IClientGrain
    {
        private const int _maxFailAttempts = 3;
        private readonly ILogger<ClientGrain> _logger;
        private IAsyncStream<string> _clientDisconnectStream;
        private int _failAttempts;
        private ConnectionGrainKey _keyData;
        private IAsyncStream<Guid> _serverDisconnectedStream;
        private StreamSubscriptionHandle<Guid> _serverDisconnectedSubscription;
        private IAsyncStream<ClientMessage> _serverStream;
        private IStreamProvider _streamProvider;

        public ClientGrain(ILogger<ClientGrain> logger)
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            _keyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(SignalrConstants.STREAM_PROVIDER);
            _clientDisconnectStream = _streamProvider.GetStream<string>(SignalrConstants.CLIENT_DISCONNECT_STREAM_ID, _keyData.Id);

            if (State.ServerId == Guid.Empty)
                return;

            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, SignalrConstants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(State.ServerId, SignalrConstants.SERVER_DISCONNECTED);
            var subscriptions = await _serverDisconnectedStream.GetAllSubscriptionHandles();
            var subscriptionTasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                subscriptionTasks.Add(subscription.ResumeAsync(async (__, _) => await OnDisconnect("server-disconnected")));
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public async Task OnConnect(Guid serverId)
        {
            State.ServerId = serverId;
            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, SignalrConstants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(State.ServerId, SignalrConstants.SERVER_DISCONNECTED);
            _serverDisconnectedSubscription = await _serverDisconnectedStream.SubscribeAsync(async _ => await OnDisconnect("server-disconnected"));
            await WriteStateAsync();
        }

        public async Task OnDisconnect(string reason = null)
        {
            _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via {reason}",
                _keyData.HubName, _keyData.Id, State.ServerId, reason);

            if (_keyData.Id != null)
            {
                await _clientDisconnectStream.OnNextAsync(_keyData.Id);
            }
            await ClearStateAsync();

            if (_serverDisconnectedSubscription != null)
                await _serverDisconnectedSubscription.UnsubscribeAsync();

            DeactivateOnIdle();
        }

        public async Task Send(Immutable<InvocationMessage> message)
        {
            if (!await TrySend(message))
            {
                _logger.LogInformation("Client not connected for connectionId {connectionId} and hub {hubName} ({targetMethod})", _keyData.Id, _keyData.HubName, message.Value.Target);

                _failAttempts++;
                if (_failAttempts >= _maxFailAttempts)
                {
                    await OnDisconnect("attempts-limit-reached");
                    _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                        _keyData.Id, _keyData.HubName, message.Value.Target);
                }
            }
        }

        public async Task<bool> TrySend(Immutable<InvocationMessage> message)
        {
            if (State.ServerId != Guid.Empty)
            {
                _logger.LogDebug("Sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Value.Target, _keyData.Id);
                _failAttempts = 0;
                await _serverStream.OnNextAsync(new ClientMessage(_keyData.Id, _keyData.HubName, message.Value));
                return true;
            }

            return false;
        }
    }

    [DebuggerDisplay("{ServerId}")]
    internal class ClientState
    {
        public Guid ServerId { get; set; }
    }
}