using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.SignalR.Core;

namespace Orleans.SignalR.Users
{
    [StorageProvider(ProviderName = SignalrConstants.STORAGE_PROVIDER)]
    [Reentrant]
    internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
    {
        public UserGrain(ILogger<UserGrain> logger) : base(logger)
        {
        }
    }

    internal class UserState : ConnectionState
    {
    }
}