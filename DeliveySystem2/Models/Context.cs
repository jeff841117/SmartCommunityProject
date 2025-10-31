using Microsoft.EntityFrameworkCore;

namespace DeliveySystem2.Models
{
    public class Context: DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
    }
}
