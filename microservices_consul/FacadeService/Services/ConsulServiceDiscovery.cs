using Consul;

namespace FacadeService.Services
{
    public class ConsulServiceDiscovery
    {
        private readonly IConsulClient _consul;

        public ConsulServiceDiscovery(IConsulClient consul) => _consul = consul;

        public async Task<List<Uri>> GetServiceEndpointsAsync(string serviceName)
        {
            var res = await _consul.Health.Service(serviceName,
                                                    tag: null,
                                                    passingOnly: true);
            return res.Response
                      .Select(svc => new Uri($"http://{svc.Service.Address}:{svc.Service.Port}"))
                      .ToList();
        }
    }
}
