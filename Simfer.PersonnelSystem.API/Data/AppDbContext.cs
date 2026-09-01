using Microsoft.EntityFrameworkCore;
using Simfer.PersonnelSystem.API.Entities;

namespace Simfer.PersonnelSystem.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<FaultCategory> FaultCategories { get; set; }
        public DbSet<UserHistory> UserHistories { get; set; }
        public DbSet<FaultyProduct> FaultyProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

            modelBuilder.Entity<FaultyProduct>()
                .HasOne(f => f.Product)
                .WithMany(p => p.FaultyProducts)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FaultyProduct>()
                .HasOne(f => f.FaultCategory)
                .WithMany(c => c.FaultyProducts)
                .HasForeignKey(f => f.FaultCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}