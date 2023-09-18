using MinimalChatApp.Context;
using MinimalChatApp.Models;
using System.Text;

namespace MinimalChatApp.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext _context)
        {
            try
            {
                // Get the IP address of the caller
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();

                // Read the request body and buffer it so it can be read multiple times
                var requestBody = await ReadRequestBodyAsync(context.Request);

                // Get the current timestamp
                var currentTime = DateTime.Now;

                // Extract the username from the authentication token if available
                var username = context.User.Identity?.Name ?? "No Auth Token";

   
                // Create a log entry
                var logEntry = new Log
                {
                    IpAddress = ipAddress,
                    RequestBody = requestBody,
                    Timestamp = currentTime,
                    Username = username
                };


                await _context.AddAsync(logEntry);
                await _context.SaveChangesAsync();
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
            
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            // Read and buffer the request body
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                var requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset the stream position to allow further processing
                return requestBody;
            }
        }
    }
}

