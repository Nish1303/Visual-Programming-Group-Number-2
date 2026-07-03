using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Data.Repositories
{
    /// <summary>
    /// All EF Core access for Sessions lives here. Uses IDbContextFactory so each
    /// operation gets its own short-lived, thread-safe DbContext instance —
    /// important since the tracker calls this from a background Task.
    /// </summary>
    public class SessionRepository : ISessionRepository
    {
        private readonly IDbContextFactory<FocusTrackDbContext> _factory;

        public SessionRepository(IDbContextFactory<FocusTrackDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<Session> AddSessionAsync(Session session)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            return session;
        }

        public async Task<List<SessionRow>> GetSessionsAsync(SessionFilter filter)
        {
            using var db = await _factory.CreateDbContextAsync();

            var query = db.Sessions
                .Include(s => s.Application).ThenInclude(a => a!.Category)
                .Where(s => s.ProfileId == filter.ProfileId)
                .AsQueryable();

            if (filter.FromDate.HasValue)
                query = query.Where(s => s.StartTime >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(s => s.StartTime <= filter.ToDate.Value);
            if (!string.IsNullOrWhiteSpace(filter.ApplicationNameContains))
                query = query.Where(s => s.Application!.DisplayName.Contains(filter.ApplicationNameContains));
            if (filter.CategoryId.HasValue)
                query = query.Where(s => s.Application!.CategoryId == filter.CategoryId.Value);

            return await query
                .OrderByDescending(s => s.StartTime)
                .Select(s => new SessionRow
                {
                    SessionId = s.Id,
                    ApplicationName = s.Application!.DisplayName,
                    CategoryName = s.Application.Category != null ? s.Application.Category.Name : "Neutral",
                    WindowTitle = s.WindowTitle,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationSeconds = (int)(s.EndTime - s.StartTime).TotalSeconds
                })
                .ToListAsync();
        }

        public async Task<List<CategoryTotal>> GetTodayTotalsAsync(int profileId)
        {
            var today = DateTime.Today;
            return await GetTotalsForRangeAsync(profileId, today, today.AddDays(1).AddSeconds(-1));
        }

        public async Task<List<CategoryTotal>> GetTotalsForRangeAsync(int profileId, DateTime from, DateTime to)
        {
            using var db = await _factory.CreateDbContextAsync();

            var raw = await db.Sessions
                .Include(s => s.Application).ThenInclude(a => a!.Category)
                .Where(s => s.ProfileId == profileId && s.StartTime >= from && s.StartTime <= to)
                .ToListAsync();

            var grouped = raw
                .GroupBy(s => new
                {
                    Id = s.Application?.Category?.Id ?? 1,
                    Name = s.Application?.Category?.Name ?? "Neutral",
                    Color = s.Application?.Category?.ColorHex ?? "#9E9E9E",
                    Goal = s.Application?.Category?.DailyGoalMinutes ?? 0
                })
                .Select(g => new CategoryTotal
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    ColorHex = g.Key.Color,
                    GoalMinutes = g.Key.Goal,
                    TotalMinutes = (int)(g.Sum(s => (s.EndTime - s.StartTime).TotalSeconds) / 60)
                })
                .OrderByDescending(c => c.TotalMinutes)
                .ToList();

            return grouped;
        }
    }
}
