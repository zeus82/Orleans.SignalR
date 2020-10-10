using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.SignalR.Core
{
    public interface IServerDirectoryGrain : IGrainWithIntegerKey
    {
        Task Heartbeat(Guid serverId);

        Task Unregister(Guid serverId);
    }

    [StorageProvider(ProviderName = SignalrConstants.STORAGE_PROVIDER)]
    public class ServerDirectoryGrain : Grain<ServerDirectoryState>, IServerDirectoryGrain
    {
        private readonly ILogger<ServerDirectoryGrain> _logger;
        private IStreamProvider _streamProvider;

        public ServerDirectoryGrain(ILogger<ServerDirectoryGrain> logger)
        {
            _logger = logger;
        }

        public Task Heartbeat(Guid serverId)
        {
            State.Servers[serverId] = DateTime.UtcNow;
            return WriteStateAsync();
        }

        public override async Task OnActivateAsync()
        {
            _streamProvider = GetStreamProvider(SignalrConstants.STREAM_PROVIDER);

            _logger.LogInformation("Available servers {serverIds}",
                string.Join(", ", State.Servers?.Count > 0 ? string.Join(", ", State.Servers) : "empty"));

            RegisterTimer(
               ValidateAndCleanUp,
               State,
               TimeSpan.FromSeconds(15),
               TimeSpan.FromMinutes(SignalrConstants.SERVERDIRECTORY_CLEANUP_IN_MINUTES));

            await base.OnActivateAsync();
        }

        public async Task Unregister(Guid serverId)
        {
            if (!State.Servers.ContainsKey(serverId))
                return;

            _logger.LogWarning("Unregister server {serverId}", serverId);
            State.Servers.Remove(serverId);
            await WriteStateAsync();
        }

        private async Task ValidateAndCleanUp(object serverDirectory)
        {
            var expiredServers = State.Servers.Where(server => server.Value < DateTime.UtcNow.AddMinutes(-SignalrConstants.SERVERDIRECTORY_CLEANUP_IN_MINUTES)).ToList();
            foreach (var server in expiredServers)
            {
                var serverDisconnectedStream = _streamProvider.GetStream<Guid>(server.Key, SignalrConstants.SERVER_DISCONNECTED);

                _logger.LogWarning("Removing server {serverId} due to inactivity {lastUpdatedDate}", server.Key, server.Value);
                await serverDisconnectedStream.OnNextAsync(server.Key);
                State.Servers.Remove(server.Key);
            }

            if (expiredServers.Count > 0)
                await WriteStateAsync();
        }
    }

    public class ServerDirectoryState
    {
        public Dictionary<Guid, DateTime> Servers { get; set; } = new Dictionary<Guid, DateTime>();
    }
}
