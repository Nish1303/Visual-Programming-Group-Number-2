using System.Collections.Generic;
using System.Threading.Tasks;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Models;

namespace FocusTrack.Data.Repositories
{
    public interface ISessionRepository
    {
        Task<Session> AddSessionAsync(Session session);
        Task<List<SessionRow>> GetSessionsAsync(SessionFilter filter);
        Task<List<CategoryTotal>> GetTodayTotalsAsync(int profileId);
        Task<List<CategoryTotal>> GetTotalsForRangeAsync(int profileId, System.DateTime from, System.DateTime to);
    }

    public interface IApplicationRepository
    {
        /// <summary>Finds an Application by executable name, creating it (as Neutral, unclassified) if new.</summary>
        Task<Application> GetOrCreateAsync(string executableName, string displayName);
        Task<List<Application>> GetAllAsync();
        Task SetCategoryAsync(int applicationId, int? categoryId);
        Task SetIgnoredAsync(int applicationId, bool isIgnored);
        Task<List<Application>> GetIgnoreListAsync();
    }

    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category> AddAsync(Category category);
        Task UpdateGoalAsync(int categoryId, int dailyGoalMinutes);
        Task<bool> HasNotifiedTodayAsync(int categoryId);
        Task RecordNotificationAsync(int categoryId);
    }
}
