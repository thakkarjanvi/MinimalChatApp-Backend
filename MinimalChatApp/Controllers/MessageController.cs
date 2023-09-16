using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalChatApp.Context;
using MinimalChatApp.Models;
using System.Net;
using System.Security.Claims;

namespace MinimalChatApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _dbcontext;
        public MessageController(ApplicationDbContext dbContext)
        {
            _dbcontext = dbContext;
        }
        // Send Message 
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessage sendMessage)
        {
            try
            {
                // Check if the required parameters are provided
                if (sendMessage == null || sendMessage.ReceiverId == Guid.Empty || string.IsNullOrWhiteSpace(sendMessage.Content))
                {
                    return BadRequest(new { error = "Message sending failed due to validation errors" });
                }

                // Get the authenticated user's ID from the token
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Unauthorized(new { error = "Unauthorized access" });
                }

                // Create a new message
                var message = new Message
                {
                    SenderId = new Guid(senderId),
                    ReceiverId = sendMessage.ReceiverId,
                    Content = sendMessage.Content,
                    Timestamp = DateTime.Now
                };
                await _dbcontext.Messages.AddAsync(message);
                await _dbcontext.SaveChangesAsync();

                // TODO: Save the message to your data store or perform message sending logic here.

                // Return a successful response with the created message
                return Ok(new
                {
                    Message = "Message sent successfully",
                    MessageId = message.MessageId,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    Timestamp = message.Timestamp
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}
