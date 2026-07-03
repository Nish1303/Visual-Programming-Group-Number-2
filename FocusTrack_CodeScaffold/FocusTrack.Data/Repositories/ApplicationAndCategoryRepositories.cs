using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusTrack.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Data.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly IDbContextFactory<FocusTrackDbContext> _factory;
        public ApplicationRepository(IDbContextFactory<FocusTrackDbContext> factory) => _factory = factory;

        public async Task<Application> GetOrCreateAsync(string executableName, string displayName)
        {
            using var db = await _factory.CreateDbContextAsync();
            var existing = await db.Applications.FirstOrDefaultAsync(a => a.ExecutableName == executableName);
            if (existing != null) return existing;

            var app = new Application
            {
                ExecutableName = executableName,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? executableName : displayName,
                CategoryId = 1, // default: Neutral
                IsIgnored = false
            };
            db.Applications.Add(app);
            await db.SaveChangesAsync();
            return app;
        }

        public async Task<List<Application>> GetAllAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Applications.Include(a => a.Category).OrderBy(a => a.DisplayName).ToListAsync();
        }

        public async Task SetCategoryAsync(int applicationId, int? categoryId)
        {
            using var db = await _factory.CreateDbContextAsync();
            var app = await db.Applications.FindAsync(applicationId);
            if (app == null) throw new InvalidOperationException($"Application {applicationId} not found.");
            app.CategoryId = categoryId ?? 1;
            await db.SaveChangesAsync();
        }

        public async Task SetIgnoredAsync(int applicationId, bool isIgnored)
        {
            using var db = await _factory.CreateDbContextAsync();
            var app = await db.Applications.FindAsync(applicationId);
            if (app == null) throw new InvalidOperationException($"Application {applicationId} not found.");
            app.IsIgnored = isIgnored;
            await db.SaveChangesAsync();
        }

        public async Task<List<Application>> GetIgnoreListAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Applications.Where(a => a.IsIgnored).ToListAsync();
        }
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbContextFactory<FocusTrackDbContext> _factory;
        public CategoryRepository(IDbContextFactory<FocusTrackDbContext> factory) => _factory = factory;

        public async Task<List<Category>> GetAllAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Category> AddAsync(Category category)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category;
        }

        public async Task UpdateGoalAsync(int categoryId, int dailyGoalMinutes)
        {
            using var db = await _factory.CreateDbContextAsync();
            var cat = await db.Categories.FindAsync(categoryId);
            if (cat == null) throw new InvalidOperationException($"Category {categoryId} not found.");
            cat.DailyGoalMinutes = dailyGoalMinutes;
            await db.SaveChangesAsync();
        }

        public async Task<bool> HasNotifiedTodayAsync(int categoryId)
        {
            using var db = await _factory.CreateDbContextAsync();
            var today = DateTime.Today;
            return await db.NotificationLogs.AnyAsync(n => n.CategoryId == categoryId && n.NotifiedForDate == today);
        }

        public async Task RecordNotificationAsync(int categoryId)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.NotificationLogs.Add(new GoalNotificationLog
            {
                CategoryId = categoryId,
                NotifiedAt = DateTime.Now,
                NotifiedForDate = DateTime.Today
            });
            await db.SaveChangesAsync();
        }
    }
}
