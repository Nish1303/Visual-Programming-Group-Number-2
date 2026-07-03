using System;

namespace FocusTrack.Data.Entities
{
    /// <summary>
    /// Records that the user was already notified for a category's goal breach today,
    /// so we don't spam a desktop notification on every poll tick.
    /// </summary>
    public class GoalNotificationLog
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime NotifiedAt { get; set; }

        /// <summary>The calendar date (date-only) this notification applies to.</summary>
        public DateTime NotifiedForDate { get; set; }
    }
}
