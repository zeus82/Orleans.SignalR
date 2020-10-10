using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.SignalR.Core;

namespace Orleans.SignalR.Groups
{
    [StorageProvider(ProviderName = SignalrConstants.STORAGE_PROVIDER)]
    [Reentrant]
    internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
    {
        public GroupGrain(ILogger<GroupGrain> logger) : base(logger)
        {
        }
    }

    internal class GroupState : ConnectionState
    {
    }
}