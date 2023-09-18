using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinimalChatApp.Models;

namespace MinimalChatApp.Context
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        } 
        public DbSet<Message> Messages { get; set; }

        public DbSet<Log> LogEntries { get; set; }

    }
}
