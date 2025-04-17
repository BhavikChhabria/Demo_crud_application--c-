using Microsoft.EntityFrameworkCore;
using MiddlewareDemo.Models;

namespace MiddlewareDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserDetail> UserDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserDetail>()
                .HasOne(ud => ud.User)
                .WithMany()   
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);  
        }
    }
}
