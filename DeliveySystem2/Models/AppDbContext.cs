using Microsoft.EntityFrameworkCore;

namespace DeliveySystem2.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
    }
}