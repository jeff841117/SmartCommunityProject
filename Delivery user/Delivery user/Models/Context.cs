using Microsoft.EntityFrameworkCore;

namespace Delivery_user.Models
{
    public class Context: DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
    }
}
