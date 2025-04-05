using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using FacadeService.Services;

namespace FacadeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacadeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ServiceDiscovery _serviceDiscovery;

        public FacadeController(IHttpClientFactory httpClientFactory, ServiceDiscovery serviceDiscovery)
        {
            _httpClient = httpClientFactory.CreateClient("LoggingServiceClient");
            _serviceDiscovery = serviceDiscovery;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            var id = Guid.NewGuid().ToString();
            var logMessage = new { Id = id, Message = request.Message };

            var endpoints = await _serviceDiscovery.GetServiceEndpointsAsync("LoggingService");
            if (endpoints == null || endpoints.Count == 0)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "No LoggingService endpoints available.");
            }

            var random = new Random();
            var endpointList = endpoints.OrderBy(x => random.Next()).ToList();

            HttpResponseMessage response = null;
            foreach (var endpoint in endpointList)
            {
                try
                {
                    var content = new StringContent(JsonSerializer.Serialize(logMessage), Encoding.UTF8, "application/json");
                    Console.WriteLine($"[FacadeService] Attempting to send message to {endpoint}: {logMessage.Message}");
                    response = await _httpClient.PostAsync($"{endpoint}/api/Logging/log", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[FacadeService] Success with endpoint {endpoint}");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"[FacadeService] Endpoint {endpoint} responded with status {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FacadeService] Error connecting to {endpoint}: {ex.Message}");
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "All LoggingService endpoints failed.");
            }

            return Ok(new { Id = id });
        }


        [HttpGet("fetch")]
        public async Task<IActionResult> FetchMessages()
        {
            Console.WriteLine("[FacadeService] Fetching messages...");

            var endpoints = await _serviceDiscovery.GetServiceEndpointsAsync("LoggingService");
            if (endpoints == null || endpoints.Count == 0)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "No LoggingService endpoints available.");
            }

            var random = new Random();
            var endpointList = endpoints.OrderBy(x => random.Next()).ToList();

            string logsResponse = null;
            foreach (var endpoint in endpointList)
            {
                try
                {
                    logsResponse = await _httpClient.GetStringAsync($"{endpoint}/api/Logging/logs");
                    Console.WriteLine($"[FacadeService] Received logs from {endpoint}: {logsResponse}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FacadeService] Error fetching logs from {endpoint}: {ex.Message}");
                }
            }

            if (logsResponse == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Failed to fetch logs from all LoggingService endpoints.");
            }

            var messageResponse = await _httpClient.GetStringAsync("http://localhost:5002/api/Messages/message");
            Console.WriteLine($"[FacadeService] Received Message from MessagesService: {messageResponse}");

            return Ok($"{logsResponse}\n{messageResponse}");
        }

    }
    public class MessageRequest
    {
        public required string Message { get; set; }
    }

}
