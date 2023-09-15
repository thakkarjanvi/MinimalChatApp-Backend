using Microsoft.AspNetCore.Identity;

namespace MinimalChatApp.Models
{
    public class User: IdentityUser
    {
        public string FullName { get; set; }
    }
}
