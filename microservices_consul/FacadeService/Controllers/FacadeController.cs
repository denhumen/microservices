using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using FacadeService.Services;
using Confluent.Kafka;
using System.Runtime.InteropServices;

namespace FacadeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacadeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ConsulServiceDiscovery _discovery;
        private readonly IProducer<Null, string> _producer;

        public FacadeController(IHttpClientFactory httpClientFactory, ConsulServiceDiscovery discovery, IProducer<Null, string> producer)
        {
            _httpClient = httpClientFactory.CreateClient("LoggingServiceClient");
            _discovery = discovery;
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

            var endpoints = await _discovery.GetServiceEndpointsAsync("LoggingService");
            if (!endpoints.Any())
                return StatusCode(503, "No LoggingService instances");

            var endpoint = endpoints[new Random().Next(endpoints.Count)];

            var baseUrl = endpoint.AbsoluteUri.TrimEnd('/');
            var fullUrl = $"{baseUrl}/api/Logging/log";

            var content = new StringContent(
                JsonSerializer.Serialize(new { Id = id, Message = request.Message }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(fullUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Facade] LoggingService at {fullUrl} returned {response.StatusCode}");
                return StatusCode(503, "LoggingService failed");
            }

            return Ok(new { Id = id });
        }


        [HttpGet("fetch")]
        public async Task<IActionResult> FetchMessages()
        {
            var rnd = new Random();

            var logEndpoints = await _discovery.GetServiceEndpointsAsync("LoggingService");
            if (!logEndpoints.Any())
                return StatusCode(503, "No LoggingService endpoints available.");

            var logEndpoint = logEndpoints[rnd.Next(logEndpoints.Count)];
            var logsUri = new Uri(logEndpoint, "api/Logging/logs");

            string rawLogs;
            try
            {
                Console.WriteLine($"[FacadeService] Fetching logs from {logsUri}");
                rawLogs = await _httpClient.GetStringAsync(logsUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacadeService] Error fetching logs: {ex.Message}");
                return StatusCode(503, "Failed to fetch logs.");
            }

            var logs = rawLogs
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var msgEndpoints = await _discovery.GetServiceEndpointsAsync("MessagesService");
            if (!msgEndpoints.Any())
                return StatusCode(503, "No MessagesService endpoints available.");

            var msgEndpoint = msgEndpoints[rnd.Next(msgEndpoints.Count)];
            var msgUri = new Uri(msgEndpoint, "api/Messages/message");

            string nextMessage;
            try
            {
                Console.WriteLine($"[FacadeService] Fetching message from {msgUri}");
                nextMessage = await _httpClient.GetStringAsync(msgUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FacadeService] Error fetching message: {ex.Message}");
                return StatusCode(503, "Failed to fetch message.");
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
