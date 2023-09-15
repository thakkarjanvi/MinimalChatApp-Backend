using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MinimalChatApp.Context;
using MinimalChatApp.Models;

namespace MinimalChatApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _dbcontext;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbcontext = dbContext;
        }

        // Register

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register request)
        { 
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Registration failed due to validation errors" });
            }

            // Check if the email is already registered
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "Registration failed because the email is already registered" });
            }

            // Create a new User
            var newUser = new User
            {
            
            Email = request.Email,
            FullName = request.Name,
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);

            if (result.Succeeded)
            {
                // Sign in the user (if needed)
                await _signInManager.SignInAsync(newUser, isPersistent: false);

                // Return a success response
                return Ok(new
                {
                    message = "Registration successful",
                    userId = newUser.Id,
                    fullname = request.Name,
                    email = newUser.Email
                });
            }
            else
            {
                // Return errors if user creation fails
                return BadRequest(new { error = "User creation failed" });
            }
        }
    }
}
