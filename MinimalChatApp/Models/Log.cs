namespace MinimalChatApp.Models
{
    public class Log
    {
        public int Id { get; set; } // Unique identifier for the log entry
        public string IpAddress { get; set; } // IP address of the caller
        public string RequestBody { get; set; } // Request body
        public DateTime Timestamp { get; set; } // Time of the call
        public string Username { get; set; } // Username fetched from the auth token
    }
}
