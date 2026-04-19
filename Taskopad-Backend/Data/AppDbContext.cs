using Microsoft.EntityFrameworkCore;
using Taskopad_Backend.Models;

namespace Taskopad_Backend.Data
{
    public class AppDbContext :DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        protected AppDbContext()
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Composite PK for join table
            builder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Unique email
            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ── Seed 5 roles 
            builder.Entity<Role>().HasData(
                new Role { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Junior" },
                new Role { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Senior" },
                new Role { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "TechLead" },
                new Role { Id = new Guid("44444444-4444-4444-4444-444444444444"), Name = "PM" },
                new Role { Id = new Guid("55555555-5555-5555-5555-555555555555"), Name = "Admin" }
            );
        }
    }
}
