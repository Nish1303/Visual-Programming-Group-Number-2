using System;

namespace FocusTrack.Data.Entities
{
    /// <summary>
    /// One contiguous block of time a given Application held the foreground window.
    /// </summary>
    public class Session
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application? Application { get; set; }

        public int ProfileId { get; set; }
        public UserProfile? Profile { get; set; }

        public string WindowTitle { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        /// <summary>Convenience — not mapped, derived from Start/End.</summary>
        public int DurationSeconds => (int)Math.Max(0, (EndTime - StartTime).TotalSeconds);
    }
}
