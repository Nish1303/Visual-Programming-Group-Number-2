using FocusTrack.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Data
{
    /// <summary>
    /// Code-First EF Core context. Only classes inside the Data layer may reference this
    /// type directly — the Business layer talks to it only through repository interfaces.
    /// </summary>
    public class FocusTrackDbContext : DbContext
    {
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<UserProfile> Profiles => Set<UserProfile>();
        public DbSet<GoalNotificationLog> NotificationLogs => Set<GoalNotificationLog>();

        public FocusTrackDbContext(DbContextOptions<FocusTrackDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Application>(e =>
            {
                e.HasIndex(a => a.ExecutableName).IsUnique();
                e.HasOne(a => a.Category)
                 .WithMany(c => c.Applications)
                 .HasForeignKey(a => a.CategoryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Session>(e =>
            {
                e.HasOne(s => s.Application)
                 .WithMany(a => a.Sessions)
                 .HasForeignKey(s => s.ApplicationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Profile)
                 .WithMany(p => p.Sessions)
                 .HasForeignKey(s => s.ProfileId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => new { s.ApplicationId, s.StartTime });
                e.Ignore(s => s.DurationSeconds);
            });

            modelBuilder.Entity<GoalNotificationLog>(e =>
            {
                e.HasOne(g => g.Category)
                 .WithMany(c => c.NotificationLogs)
                 .HasForeignKey(g => g.CategoryId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Prevent duplicate notifications for the same category on the same day.
                e.HasIndex(g => new { g.CategoryId, g.NotifiedForDate }).IsUnique();
            });

            // Seed the mandatory default category so unclassified apps always resolve.
            modelBuilder.Entity<Category>().HasData(new Category
            {
                Id = 1,
                Name = "Neutral",
                ColorHex = "#9E9E9E",
                DailyGoalMinutes = 0,
                IsSystemDefault = true
            });

            modelBuilder.Entity<UserProfile>().HasData(new UserProfile
            {
                Id = 1,
                ProfileName = "Default",
                CreatedAt = new System.DateTime(2025, 1, 1),
                IsActive = true
            });
        }
    }
}
