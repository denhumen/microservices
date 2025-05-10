using ConfigServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace ConfigServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> ServiceRegistry = new();

        [HttpPost("register")]
        public IActionResult RegisterService([FromBody] RegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ServiceName) || string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest("ServiceName and Url must be provided.");
            }

            var endpoints = ServiceRegistry.GetOrAdd(request.ServiceName, _ => new HashSet<string>());
            lock (endpoints)
            {
                endpoints.Add(request.Url);
            }
            return Ok(new { Message = "Service registered successfully." });
        }

        [HttpPost("unregister")]
        public IActionResult UnregisterService([FromBody] RegistrationRequest request)
        {
            if (ServiceRegistry.TryGetValue(request.ServiceName, out var endpoints))
            {
                lock (endpoints)
                {
                    endpoints.Remove(request.Url);
                }
                return Ok(new { Message = "Service unregistered successfully." });
            }
            return NotFound($"Service {request.ServiceName} not found.");
        }

        [HttpGet("{serviceName}")]
        public IActionResult GetServiceEndpoints(string serviceName)
        {
            if (ServiceRegistry.TryGetValue(serviceName, out var endpoints) && endpoints.Any())
            {
                return Ok(endpoints);
            }
            return NotFound($"No endpoints registered for service {serviceName}.");
        }   
    }
}
