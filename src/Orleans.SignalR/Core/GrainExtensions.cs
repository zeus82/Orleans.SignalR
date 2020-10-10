using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;
using Orleans.SignalR.Clients;
using Orleans.SignalR.Core;
using Orleans.SignalR.Groups;
using Orleans.SignalR.Users;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class GrainFactoryExtensions
    {
        public static HubContext<THub> GetHub<THub>(this IGrainFactory grainFactory)
        {
            return new HubContext<THub>(grainFactory);
        }

        internal static IClientGrain GetClientGrain(this IGrainFactory factory, string hubName, string connectionId)
            => factory.GetGrain<IClientGrain>(ConnectionGrainKey.Build(hubName, connectionId));

        internal static IGroupGrain GetGroupGrain(this IGrainFactory factory, string hubName, string groupName)
            => factory.GetGrain<IGroupGrain>(ConnectionGrainKey.Build(hubName, groupName));

        internal static IServerDirectoryGrain GetServerDirectoryGrain(this IGrainFactory factory)
            => factory.GetGrain<IServerDirectoryGrain>(0);

        internal static IUserGrain GetUserGrain(this IGrainFactory factory, string hubName, string userId)
                    => factory.GetGrain<IUserGrain>(ConnectionGrainKey.Build(hubName, userId));
    }

    public static class GrainSignalRExtensions
    {
        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        public static Task Send(this IHubMessageInvoker grain, string methodName, params object[] args)
        {
            var invocationMessage = new InvocationMessage(methodName, args).AsImmutable();
            return grain.Send(invocationMessage);
        }

        /// <summary>
        /// Invokes a method on the hub (one way).
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        public static void SendOneWay(this IHubMessageInvoker grain, string methodName, params object[] args)
        {
            grain.InvokeOneWay(g => g.Send(methodName, args));
        }

        [Obsolete("Use Send instead", false)]
        public static async Task SendSignalRMessage(this IConnectionGrain grain, string methodName, params object[] message)
        {
            var invocationMessage = new InvocationMessage(methodName, message).AsImmutable();
            await grain.Send(invocationMessage);
        }

        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        /// <returns>true if the message was sent successfully</returns>
        public static Task<bool> TrySend(this IClientGrain grain, string methodName, params object[] args)
        {
            var invocationMessage = new InvocationMessage(methodName, args).AsImmutable();
            return grain.TrySend(invocationMessage);
        }
    }
}