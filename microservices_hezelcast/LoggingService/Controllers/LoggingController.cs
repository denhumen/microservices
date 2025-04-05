using Hazelcast.DistributedObjects;
using LoggingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoggingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoggingController : Controller
    {
        private readonly IHMap<string, string> _logMap;

        public LoggingController(HazelcastService hazelcastService)
        {
            _logMap = hazelcastService.LogMap;
        }


        [HttpPost("log")]
        public async Task<IActionResult> LogMessageAsync([FromBody] LogEntry entry)
        {
            if (await _logMap.ContainsKeyAsync(entry.Id))
            {
                Console.WriteLine($"[LoggingService] Duplicate message detected: {entry.Id}");
                return Conflict("Duplicate message detected.");
            }
            await _logMap.SetAsync(entry.Id, entry.Message);
            Console.WriteLine($"[LoggingService] New log received. ID: {entry.Id}, Message: {entry.Message}");
            return Ok();
        }


        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            Console.WriteLine("[LoggingService] Sending stored logs...");
            var logs = await _logMap.GetValuesAsync();
            return Ok(string.Join("\n", logs));
        }

    }
    public class LogEntry
    {
        public required string Id { get; set; }
        public required string Message { get; set; }
    }

}
