using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace SignalR.Server.Data
{
    public class SignalRServerDbContext : DbContext
    {
        public SignalRServerDbContext(DbContextOptions<SignalRServerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        // Add other DbSets as needed
    }
}
