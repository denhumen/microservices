using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace FacadeService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacadeController : Controller
    {
        private readonly HttpClient _httpClient;

        public FacadeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("LoggingServiceClient");
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            var id = Guid.NewGuid().ToString();
            var logMessage = new { Id = id, Message = request.Message };

            var content = new StringContent(JsonSerializer.Serialize(logMessage), Encoding.UTF8, "application/json");
            Console.WriteLine($"[FacadeService] Sending message to LoggingService: {logMessage.Message}");

            var response = await _httpClient.PostAsync("http://localhost:5001/api/Logging/log", content);

            Console.WriteLine($"[FacadeService] LoggingService Response: {response.StatusCode}");
            return Ok(new { Id = id });
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> FetchMessages()
        {
            Console.WriteLine("[FacadeService] Fetching messages...");

            var logsResponse = await _httpClient.GetStringAsync("http://localhost:5001/api/Logging/logs");
            Console.WriteLine($"[FacadeService] Received Logs: {logsResponse}");

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
