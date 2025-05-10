using Hazelcast.DistributedObjects;
using Hazelcast;
using System;

namespace LoggingService.Services
{
    public class HazelcastService
    {
        public IHazelcastClient Client { get; private set; }
        public IHMap<string, string> LogMap { get; private set; }

        public async Task InitializeAsync()
        {
            Client = await HazelcastClientFactory.StartNewClientAsync(options => { });
            LogMap = await Client.GetMapAsync<string, string>("logs");
        }
    }
}
