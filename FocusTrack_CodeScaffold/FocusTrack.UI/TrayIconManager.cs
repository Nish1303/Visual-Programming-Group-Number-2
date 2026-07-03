using System;
using System.Windows.Forms;

namespace FocusTrack.UI
{
    /// <summary>
    /// Owns the single NotifyIcon for the app lifetime. Registered as its own DI singleton
    /// (independent of MainForm) so NotificationService (Business layer) and MainForm (UI layer)
    /// can both depend on it without a circular resolution.
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        public NotifyIcon Icon { get; }
        public event EventHandler? ShowRequested;
        public event EventHandler? HideRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager()
        {
            var menu = new ContextMenuStrip();
            var showItem = menu.Items.Add("Show FocusTrack");
            var hideItem = menu.Items.Add("Hide");
            menu.Items.Add(new ToolStripSeparator());
            var exitItem = menu.Items.Add("Exit");

            showItem.Click += (_, __) => ShowRequested?.Invoke(this, EventArgs.Empty);
            hideItem.Click += (_, __) => HideRequested?.Invoke(this, EventArgs.Empty);
            exitItem.Click += (_, __) => ExitRequested?.Invoke(this, EventArgs.Empty);

            Icon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "FocusTrack",
                Visible = true,
                ContextMenuStrip = menu
            };

            Icon.DoubleClick += (_, __) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Icon.Visible = false;
            Icon.Dispose();
        }
    }
}
