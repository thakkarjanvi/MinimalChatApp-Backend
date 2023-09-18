using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalChatApp.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalChatApp.Controllers
{
    [Route("api/log")]
    [ApiController]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public LogController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
        {
            try
            {

                // Validate the request parameters
                if (startTime == null) startTime = DateTime.Now.AddMinutes(-5); // Default: Current Timestamp - 5 minutes
                if (endTime == null) endTime = DateTime.Now; // Default: Current Timestamp

                if (startTime >= endTime)
                {
                    return BadRequest(new { error = "Invalid request parameters" });
                }

                // Fetch log entries based on the specified time range
                var logs = await _dbContext.LogEntries.Where(log => log.Timestamp >= startTime && log.Timestamp <= endTime)
                                                      .ToListAsync();

                if (logs.Count == 0)
                {
                    return NotFound(new { error = "No logs found" });
                }

                return Ok(new { message = "Log list received successfully", logs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
