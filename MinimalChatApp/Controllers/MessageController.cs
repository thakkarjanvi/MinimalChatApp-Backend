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

        //Edit Message
        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessage editMessage)
        {
            try
            {
                // Check if the required parameters are provided
                if (editMessage == null)
                {
                    return BadRequest(new { error = "Message editing failed due to validation errors" });
                }

                // Get the authenticated user's ID from the token
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Unauthorized(new { error = "Unauthorized access" });
                }

                // Find the message to edit in the database
                var messageToUpdate = await _dbcontext.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);

                if (messageToUpdate == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                // Check if the authenticated user is the sender of the message
                if (messageToUpdate.SenderId.ToString() != senderId)
                {
                    return Unauthorized(new { error = "You are not authorized to edit this message" });
                }

                // Update the message content
                messageToUpdate.Content = editMessage.Content;
                messageToUpdate.Timestamp = DateTime.Now;

                // Save the changes to the database
                await _dbcontext.SaveChangesAsync();

                // Return a successful response with the updated message
                return Ok(new
                {
                    Message = "Message edited successfully",
                    MessageId = messageToUpdate.MessageId,
                    SenderId = messageToUpdate.SenderId,
                    ReceiverId = messageToUpdate.ReceiverId,
                    Content = messageToUpdate.Content,
                    Timestamp = messageToUpdate.Timestamp
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // Delete Message

        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                // Get the authenticated user's ID from the token
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Unauthorized(new { error = "Unauthorized access" });
                }


                // Find the message to delete in the database
                var messageToDelete = await _dbcontext.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);

                if (messageToDelete == null)
                {
                    return NotFound(new { error = "Message not found" });
                }

                // Check if the authenticated user is the sender of the message
                if (!messageToDelete.SenderId.Equals(new Guid(senderId)))
                {
                    return Unauthorized(new { error = "You are not authorized to delete this message" });
                }

                // Remove the message from the database
                _dbcontext.Messages.Remove(messageToDelete);
                await _dbcontext.SaveChangesAsync();

                // Return a successful response
                return Ok(new { Message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        // Retrieve Conversation History 
        [HttpGet("messages")]
        public async Task<IActionResult> RetrieveConversationHistory(
            [FromQuery] Guid userId,
            [FromQuery] DateTime? before = null,
            [FromQuery] int count = 20,
            [FromQuery] string sort = "asc")
        {
            try
            {
                // Get the authenticated user's ID from the token
                var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Unauthorized(new { error = "Unauthorized access" });
                }

                // Find the user in the database
                var user = await _dbcontext.Users.FirstOrDefaultAsync(u => u.Id == userId.ToString());

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Check if the authenticated user is authorized to retrieve this conversation
                if (user.Id != new Guid(senderId).ToString())
                {
                    return Unauthorized(new { error = "You are not authorized to retrieve this conversation" });
                }

                // Retrieve conversation messages based on the provided parameters
                var messagesQuery = _dbcontext.Messages
                    .Where(m => (m.SenderId == new Guid(senderId) && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == new Guid(senderId)));

                if (before.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.Timestamp < before);
                }

                if (sort == "asc")
                {
                    messagesQuery = messagesQuery.OrderBy(m => m.Timestamp);
                }
                else
                {
                    messagesQuery = messagesQuery.OrderByDescending(m => m.Timestamp);
                }

                var conversationMessages = await messagesQuery.Take(count).ToListAsync();

                // Prepare the response body
                var response = new
                {
                    messages = conversationMessages.Select(m => new
                    {
                        id = m.MessageId,
                        senderId = m.SenderId,
                        receiverId = m.ReceiverId,
                        content = m.Content,
                        timestamp = m.Timestamp
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}
