using System.Collections.Generic;
using WebApplication3.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication4.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
    }
}