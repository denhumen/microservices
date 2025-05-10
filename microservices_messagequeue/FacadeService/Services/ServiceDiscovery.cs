using System.Text.Json;

namespace FacadeService.Services
{
    public class ServiceDiscovery
    {
        private readonly HttpClient _httpClient;
        private const string ConfigPath = "/api/config";

        public ServiceDiscovery(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetServiceEndpointsAsync(string serviceName)
        {
            // base address is already http://config-server:5001
            var response = await _httpClient
                                 .GetAsync($"{ConfigPath}/{serviceName}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer
                   .Deserialize<List<string>>(content)
                   ?? throw new InvalidOperationException("Bad payload");
        }
    }
}
