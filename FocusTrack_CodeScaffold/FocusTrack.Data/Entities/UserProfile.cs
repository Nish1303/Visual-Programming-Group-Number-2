using System;
using System.Collections.Generic;

namespace FocusTrack.Data.Entities
{
    /// <summary>
    /// Supports the optional "Multi-user profile" feature: all Sessions are scoped
    /// to a Profile so switching profiles switches the tracked/reported data.
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = "Default";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public List<Session> Sessions { get; set; } = new();
    }
}
