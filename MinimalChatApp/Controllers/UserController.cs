using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MinimalChatApp.Context;
using MinimalChatApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MinimalChatApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _dbcontext;
        private readonly IConfiguration _configuration;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbcontext = dbContext;
            _configuration = configuration;
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
            UserName = request.Email,
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

        // Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            // Validate the model
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Login failed due to validation errors" });
            }

            // Authenticate the user
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { error = "Login failed due to incorrect credentials" });
            }

            // Generate and return a JWT token
            var token = GenerateJwtToken(user);

            return Ok(new { message = "Login successfully done", token,
                profile = new
                {
                    id = user.Id,
                    name = user.FullName,
                    email = user.Email
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:DurationInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Retrieve User List 
        [HttpGet("users")]
        [Authorize] // Requires authentication to access this endpoint
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Retrieve the list of users (excluding the current user)
                var currentUser = _userManager.GetUserAsync(User).Result; // Get the current user
                var users = _userManager.Users.Where(u => u.Id != currentUser.Id)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FullName,
                        email = u.Email
                    })
                    .ToList();

                return Ok(new { message = "User Retrieve List successfully done", users });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
