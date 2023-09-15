using System.ComponentModel.DataAnnotations;

namespace MinimalChatApp.Models
{
    public class Register
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [MinLength(6)] // Define password requirements as needed
        public string Password { get; set; }
    }
}
