using System.Text.Json;

namespace FacadeService.Services
{
    public class ServiceDiscovery
    {
        private readonly HttpClient _httpClient;
        private const string ConfigServerUrl = "http://localhost:5001/api/config";
        public ServiceDiscovery(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetServiceEndpointsAsync(string serviceName)
        {
            var response = await _httpClient.GetAsync($"{ConfigServerUrl}/{serviceName}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content);
        }
    }
}
