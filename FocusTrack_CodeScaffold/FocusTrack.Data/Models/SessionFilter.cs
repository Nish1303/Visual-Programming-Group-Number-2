using System;

namespace FocusTrack.Data.Models
{
    /// <summary>Filter criteria for the History Browser (date range, application, category).</summary>
    public class SessionFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ApplicationNameContains { get; set; }
        public int? CategoryId { get; set; }
        public int ProfileId { get; set; } = 1;
    }

    /// <summary>Row shown in the tabular report / history grid.</summary>
    public class SessionRow
    {
        public int SessionId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationSeconds { get; set; }
    }

    /// <summary>Aggregated total used by the Dashboard chart and reports.</summary>
    public class CategoryTotal
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#808080";
        public int TotalMinutes { get; set; }
        public int GoalMinutes { get; set; }
    }
}
