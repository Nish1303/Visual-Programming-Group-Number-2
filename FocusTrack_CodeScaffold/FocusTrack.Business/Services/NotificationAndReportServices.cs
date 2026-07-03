using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Models;
using FocusTrack.Data.Repositories;

namespace FocusTrack.Business.Services
{
    /// <summary>
    /// Wraps the desktop notification mechanism (NotifyIcon balloon tip / Windows toast).
    /// Kept in the Business layer behind INotificationService so CategoryService can be
    /// unit-tested with a mock notifier.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly NotifyIcon _trayIcon;

        public NotificationService(NotifyIcon trayIcon)
        {
            _trayIcon = trayIcon;
        }

        public void ShowGoalExceededNotification(Category category, int minutesSpent)
        {
            _trayIcon.BalloonTipTitle = "Daily goal exceeded";
            _trayIcon.BalloonTipText =
                $"You've spent {minutesSpent} min on \"{category.Name}\" today " +
                $"(goal: {category.DailyGoalMinutes} min).";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
            _trayIcon.ShowBalloonTip(5000);
        }
    }

    public class ReportService : IReportService
    {
        private readonly ISessionRepository _sessionRepository;
        public ReportService(ISessionRepository sessionRepository) => _sessionRepository = sessionRepository;

        /// <summary>Tabular report: sessions grouped by application/category with subtotals — grouping done in the UI/report renderer using this raw row set.</summary>
        public Task<List<SessionRow>> GetTabularReportAsync(SessionFilter filter)
            => _sessionRepository.GetSessionsAsync(filter);

        /// <summary>Chart-based report: totals per category for an arbitrary date range (daily or weekly).</summary>
        public Task<List<CategoryTotal>> GetChartReportAsync(int profileId, DateTime from, DateTime to)
            => _sessionRepository.GetTotalsForRangeAsync(profileId, from, to);
    }
}
