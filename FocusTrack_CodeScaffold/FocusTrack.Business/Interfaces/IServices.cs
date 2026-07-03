using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Models;

namespace FocusTrack.Business.Interfaces
{
    /// <summary>Raised on the background thread every time a foreground session is closed and saved.</summary>
    public class SessionRecordedEventArgs : EventArgs
    {
        public string ApplicationName { get; init; } = string.Empty;
        public int DurationSeconds { get; init; }
    }

    public interface IWindowTrackerService
    {
        event EventHandler<SessionRecordedEventArgs>? SessionRecorded;
        bool IsRunning { get; }
        Task StartTrackingAsync(CancellationToken cancellationToken);
        Task StopTrackingAsync();
    }

    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<Category> AddCategoryAsync(string name, string colorHex);
        Task ClassifyApplicationAsync(int applicationId, int? categoryId);
        Task SetIgnoredAsync(int applicationId, bool isIgnored);
        Task SetDailyGoalAsync(int categoryId, int minutes);

        /// <summary>Checks today's totals against each category's goal and fires a notification once per day if exceeded.</summary>
        Task CheckGoalsAsync(int profileId);
    }

    public interface IReportService
    {
        Task<List<SessionRow>> GetTabularReportAsync(SessionFilter filter);
        Task<List<CategoryTotal>> GetChartReportAsync(int profileId, DateTime from, DateTime to);
    }

    public interface INotificationService
    {
        void ShowGoalExceededNotification(Category category, int minutesSpent);
    }
}
