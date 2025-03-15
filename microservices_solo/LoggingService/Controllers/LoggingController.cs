using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoggingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoggingController : Controller
    {
        private static readonly Dictionary<string, string> logs = new();

        [HttpPost("log")]
        public IActionResult LogMessage([FromBody] LogEntry entry)
        {
            if (logs.ContainsKey(entry.Id))
            {
                Console.WriteLine($"[LoggingService] Duplicate message detected: {entry.Id}");
                return Conflict("Duplicate message detected.");
            }

            logs[entry.Id] = entry.Message;
            Console.WriteLine($"[LoggingService] New log received. ID: {entry.Id}, Message: {entry.Message}");
            return Ok();
        }


        [HttpGet("logs")]
        public IActionResult GetLogs()
        {
            Console.WriteLine("[LoggingService] Sending stored logs...");
            return Ok(string.Join("\n", logs.Values));
        }

    }
    public class LogEntry
    {
        public required string Id { get; set; }
        public required string Message { get; set; }
    }

}
