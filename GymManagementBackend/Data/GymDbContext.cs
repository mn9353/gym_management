using Microsoft.EntityFrameworkCore;
using GymManagementBackend.Models;

namespace GymManagementBackend.Data
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
        {
        }

        public DbSet<Gym> Gyms { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gym Configuration
            modelBuilder.Entity<Gym>()
                .HasIndex(g => g.Email)
                .IsUnique();

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Users)
                .WithOne(u => u.Gym)
                .HasForeignKey(u => u.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Members)
                .WithOne(m => m.Gym)
                .HasForeignKey(m => m.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Payments)
                .WithOne(p => p.Gym)
                .HasForeignKey(p => p.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            // User Configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion(
                    v => v,
                    v => v);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Member Configuration
            modelBuilder.Entity<Member>()
                .HasMany(m => m.Payments)
                .WithOne(p => p.Member)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment Configuration - explicit foreign key setup
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Gym)
                .WithMany(g => g.Payments)
                .HasForeignKey(p => p.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            // Refresh Token Configuration
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => new { rt.UserId, rt.ExpiresAt });
        }
    }
}
