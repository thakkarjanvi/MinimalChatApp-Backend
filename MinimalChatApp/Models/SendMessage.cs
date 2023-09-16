namespace MinimalChatApp.Models
{
    public class SendMessage
    {
        public Guid ReceiverId { get; set; }
        public string Content { get; set; }
    }
}
