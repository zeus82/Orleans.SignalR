using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;

namespace Orleans.SignalR.Clients
{
    [Immutable]
    public class ClientMessage
    {
        public ClientMessage(string connectionId, string hubName, InvocationMessage payload)
        {
            ConnectionId = connectionId;
            HubName = hubName;
            Payload = payload;
        }

        public string ConnectionId { get; }
        public string HubName { get; }
        public InvocationMessage Payload { get; }
    }
}
