using Microsoft.AspNetCore.Mvc;

namespace MessagesService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private static readonly List<string> messages = new();

        [HttpPost("store")]
        public IActionResult StoreMessage([FromBody] MessageEntry entry)
        {
            messages.Add(entry.Message);
            Console.WriteLine($"[MessagesService] Stored new message: {entry.Message}");
            return Ok();
        }

        [HttpGet("message")]
        public IActionResult GetMessage()
        {
            Console.WriteLine("[MessagesService] Fetching messages...");
            if (messages.Count == 0)
            {
                return Ok("No messages available.");
            }
            return Ok(messages[^1]);
        }
    }

    public class MessageEntry
    {
        public required string Message { get; set; }
    }
}
