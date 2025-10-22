using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Models;

namespace SmartCommunity.Data
{
    public class SmartCommunityContext : IdentityDbContext<IdentityUser>
    {
        public SmartCommunityContext(DbContextOptions<SmartCommunityContext> options)
            : base(options) { }

        // 🔁 改名，避免和 Identity 的 Users 衝突
        public DbSet<User> Residents { get; set; }
        public DbSet<FeeItem> FeeItems { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 你的住戶表仍然映射到資料庫中的 "Users" 資料表
            modelBuilder.Entity<User>().ToTable("Users").HasKey(x => x.UserID);
            modelBuilder.Entity<FeeItem>().ToTable("FeeItems").HasKey(x => x.FeeItemID);
            modelBuilder.Entity<Bill>().ToTable("Bills").HasKey(x => x.BillID);
            modelBuilder.Entity<Payment>().ToTable("Payments").HasKey(x => x.PaymentID);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.User).WithMany(u => u.Bills)
                .HasForeignKey(b => b.UserID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.FeeItem).WithMany()
                .HasForeignKey(b => b.FeeItemID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bill).WithMany()
                .HasForeignKey(p => p.BillID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bill>()
                .Property(b => b.Status).HasMaxLength(20).HasDefaultValue("未繳");
        }
    }
}