using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;
using Orleans.SignalR.Core;
using System;
using System.Threading.Tasks;

namespace Orleans.SignalR.Clients
{
    public interface IClientGrain : IHubMessageInvoker, IGrainWithStringKey
    {
        Task OnConnect(Guid serverId);

        Task OnDisconnect(string reason = null);

        Task<bool> TrySend(Immutable<InvocationMessage> message);
    }
}