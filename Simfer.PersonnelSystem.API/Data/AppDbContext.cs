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
        public DbSet<UserHistory> UserHistories { get; set; }
        public DbSet<FaultyProduct> FaultyProducts { get; set; }
    }
}