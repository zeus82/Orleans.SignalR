using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;

namespace Orleans.SignalR.Tests
{
    public class OrleansFixture : IDisposable
    {
        public OrleansFixture()
        {
            var silo = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .AddMemoryGrainStorage(SignalrConstants.PUBSUB_PROVIDER)
                .AddMemoryGrainStorage("PubSubStore")
                .UseSignalR()
                .Build();
            silo.StartAsync().Wait();
            Silo = silo;

            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .UseSignalR()
                .Build();

            client.Connect().Wait();
            ClientProvider = new DefaultClusterClientProvider(client);
        }

        public IClusterClientProvider ClientProvider { get; }
        public ISiloHost Silo { get; }

        public void Dispose()
        {
            ClientProvider.GetClient().Close().Wait();
            Silo.StopAsync().Wait();
        }
    }
}