using System.Collections.Generic;

namespace FocusTrack.Data.Entities
{
    /// <summary>
    /// A user-defined bucket that applications are classified into
    /// (e.g. Productive, Neutral, Distracting) with an optional daily time goal.
    /// </summary>
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Hex color used in charts/UI, e.g. "#4CAF50".</summary>
        public string ColorHex { get; set; } = "#808080";

        /// <summary>0 = no goal set for this category.</summary>
        public int DailyGoalMinutes { get; set; }

        public bool IsSystemDefault { get; set; } = false;

        public List<Application> Applications { get; set; } = new();
        public List<GoalNotificationLog> NotificationLogs { get; set; } = new();
    }
}
