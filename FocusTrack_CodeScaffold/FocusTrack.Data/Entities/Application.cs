using System.Collections.Generic;

namespace FocusTrack.Data.Entities
{
    /// <summary>
    /// A distinct executable that has been seen in the foreground at least once.
    /// </summary>
    public class Application
    {
        public int Id { get; set; }

        /// <summary>e.g. "chrome.exe" — used as the natural lookup key when a new session starts.</summary>
        public string ExecutableName { get; set; } = string.Empty;

        /// <summary>User- or auto-derived friendly name, e.g. "Google Chrome".</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>FK — nullable until classified; defaults to the "Neutral" category in code.</summary>
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        /// <summary>Applications on the ignore list are never persisted as sessions.</summary>
        public bool IsIgnored { get; set; } = false;

        public List<Session> Sessions { get; set; } = new();
    }
}
