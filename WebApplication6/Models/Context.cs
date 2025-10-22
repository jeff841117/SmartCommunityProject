using Microsoft.EntityFrameworkCore;
using System;
using 系統端.Models;

namespace WebApplication6.Models
{
    public class Context: DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options) { }

        public DbSet<Package> Packages { get; set; }
    }
}
