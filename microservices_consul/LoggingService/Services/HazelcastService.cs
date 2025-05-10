using Hazelcast.DistributedObjects;
using Hazelcast;
using System;
using Consul;
using System.Text;

namespace LoggingService.Services
{
    public class HazelcastService
    {
        public IConsulClient Consul;
        public IHazelcastClient Client { get; private set; }
        public IHMap<string, string> LogMap { get; private set; }

        public HazelcastService(IConsulClient consul)
        {
            Consul = consul;
        }

        public async Task InitializeAsync()
        {
            var kv = Consul.KV;

            var clusterResp = await kv.Get("config/hazelcast/ClusterName");
            var clusterName = Encoding.UTF8.GetString(clusterResp.Response.Value);

            var addrResp = await kv.Get("config/hazelcast/Networking/Addresses/0");
            var address = Encoding.UTF8.GetString(addrResp.Response.Value);

            var options = new HazelcastOptionsBuilder()
                .With(o =>
                {
                    o.ClusterName = clusterName;
                    o.Networking.Addresses.Add(address);
                })
                .Build();


            Client = await HazelcastClientFactory.StartNewClientAsync(options => { });
            LogMap = await Client.GetMapAsync<string, string>("logs");
        }


    }
}
