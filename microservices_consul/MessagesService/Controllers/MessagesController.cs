using MessagesService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessagesService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        [HttpGet("message")]
        public IActionResult GetMessage()
        {
            var msgs = KafkaConsumerService.GetAll();
            return Ok(msgs);
        }
    }

    public class MessageEntry
    {
        public required string Message { get; set; }
    }
}
