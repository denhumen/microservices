using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using FacadeService.Services;
using Confluent.Kafka;

namespace FacadeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacadeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ServiceDiscovery _serviceDiscovery;
        private readonly IProducer<Null, string> _producer;

        public FacadeController(IHttpClientFactory httpClientFactory, ServiceDiscovery serviceDiscovery, IProducer<Null, string> producer)
        {
            _httpClient = httpClientFactory.CreateClient("LoggingServiceClient");
            _serviceDiscovery = serviceDiscovery;
            _producer = producer;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            var id = Guid.NewGuid().ToString();

            var logMessage = new { Id = id, Message = request.Message };

            await _producer.ProduceAsync("messages", new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(logMessage)
            });
            Console.WriteLine($"[Facade] Produced to Kafka: {request.Message}");

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

            var rnd = new Random();

            var logEndpoints = await _serviceDiscovery.GetServiceEndpointsAsync("LoggingService");
            if (logEndpoints == null || !logEndpoints.Any())
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "No LoggingService endpoints available.");

            var logEndpoint = logEndpoints.OrderBy(_ => rnd.Next()).First();
            string rawLogs;
            try
            {
                rawLogs = await _httpClient.GetStringAsync($"{logEndpoint}/api/Logging/logs");
                Console.WriteLine($"[FacadeService] Received logs from {logEndpoint}: {rawLogs}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacadeService] Error fetching logs: {ex.Message}");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Failed to fetch logs.");
            }

            var logs = rawLogs
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var msgEndpoints = await _serviceDiscovery.GetServiceEndpointsAsync("MessagesService");
            if (msgEndpoints == null || !msgEndpoints.Any())
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "No MessagesService endpoints available.");

            var msgEndpoint = msgEndpoints.OrderBy(_ => rnd.Next()).First();
            string nextMessage;
            try
            {
                nextMessage = await _httpClient.GetStringAsync($"{msgEndpoint}/api/Messages/message");
                Console.WriteLine($"[FacadeService] Received message from {msgEndpoint}: {nextMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacadeService] Error fetching message: {ex.Message}");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Failed to fetch message.");
            }

            return Ok(new
            {
                Logs = logs,
                NextMessage = nextMessage
            });
        }

    }
    public class MessageRequest
    {
        public required string Message { get; set; }
    }

}
