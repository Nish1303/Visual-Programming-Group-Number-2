using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using FocusTrack.Business.Interfaces;
using FocusTrack.UI.Forms;

namespace FocusTrack.UI
{
    /// <summary>
    /// Application shell. Pure UI concerns only: navigation between the Dashboard, History,
    /// Settings and Reports views, plus wiring the tray icon's show/hide/exit actions and
    /// starting/stopping the background tracker. No business logic or DbContext usage here.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly IWindowTrackerService _trackerService;
        private readonly ICategoryService _categoryService;
        private readonly TrayIconManager _trayIconManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _appCts = new();

        private readonly Panel _headerBar = new()
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Theme.Primary,
        };

        private readonly Label _trackingStatusLabel = new()
        {
            Text = "\u25CF  Tracking active",
            ForeColor = Color.FromArgb(180, 255, 200),
            Font = Theme.FontSmall,
            AutoSize = true,
        };

        private readonly TabControl _tabControl = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ItemSize = new Size(150, 34),
            SizeMode = TabSizeMode.Fixed,
            Appearance = TabAppearance.Normal,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            Padding = new Point(16, 6),
        };

        public MainForm(
            IWindowTrackerService trackerService,
            ICategoryService categoryService,
            TrayIconManager trayIconManager,
            IServiceProvider serviceProvider)
        {
            _trackerService = trackerService;
            _categoryService = categoryService;
            _trayIconManager = trayIconManager;
            _serviceProvider = serviceProvider;

            InitializeComponent();
            BackColor = Theme.PageBackground;

            BuildMenu();
            BuildHeader();
            BuildTabs();
            WireTrayIcon();

            Load += async (_, __) => await StartTrackingAsync();
            FormClosing += async (s, e) => await OnFormClosingAsync(e);
        }

        private void BuildMenu()
        {
            var menu = new MenuStrip { Dock = DockStyle.Top, BackColor = Theme.PrimaryDark, ForeColor = Color.White, Renderer = new DarkMenuRenderer() };
            var fileMenu = new ToolStripMenuItem("File") { ForeColor = Color.White };
            fileMenu.DropDownItems.Add("Minimise to tray", null, (_, __) => Hide());
            fileMenu.DropDownItems.Add("Exit", null, async (_, __) => await ExitApplicationAsync());
            menu.Items.Add(fileMenu);

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private void BuildHeader()
        {
            var title = new Label
            {
                Text = "FocusTrack",
                Font = Theme.FontHeading,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 14),
            };
            _trackingStatusLabel.Location = new Point(20, 36);

            _headerBar.Controls.Add(title);
            _headerBar.Controls.Add(_trackingStatusLabel);
            Controls.Add(_headerBar);
        }

        private void BuildTabs()
        {
            _tabControl.TabPages.Add(new TabPage("Dashboard") { Controls = { CreateHosted<DashboardForm>() }, BackColor = Theme.PageBackground });
            _tabControl.TabPages.Add(new TabPage("History") { Controls = { CreateHosted<HistoryForm>() }, BackColor = Theme.PageBackground });
            _tabControl.TabPages.Add(new TabPage("Reports") { Controls = { CreateHosted<ReportsForm>() }, BackColor = Theme.PageBackground });
            _tabControl.TabPages.Add(new TabPage("Settings") { Controls = { CreateHosted<SettingsForm>() }, BackColor = Theme.PageBackground });

            _tabControl.DrawItem += TabControl_DrawItem;
            Controls.Add(_tabControl);
            _tabControl.BringToFront();
        }

        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var page = _tabControl.TabPages[e.Index];
            bool selected = e.Index == _tabControl.SelectedIndex;

            using var back = new SolidBrush(selected ? Theme.Primary : Theme.CardBackground);
            e.Graphics.FillRectangle(back, e.Bounds);

            var textColor = selected ? Color.White : Theme.TextPrimary;
            TextRenderer.DrawText(e.Graphics, page.Text, _tabControl.Font, e.Bounds, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>Hosts a UserControl view inline inside a TabPage — plain control embedding,
        /// unlike a nested Form, has no invisible chrome and cannot collide with the tab strip.</summary>
        private Control CreateHosted<TControl>() where TControl : UserControl
        {
            var control = (TControl)_serviceProvider.GetService(typeof(TControl))!;
            control.Dock = DockStyle.Fill;
            return control;
        }

        private void WireTrayIcon()
        {
            _trayIconManager.ShowRequested += (_, __) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
            _trayIconManager.HideRequested += (_, __) => Hide();
            _trayIconManager.ExitRequested += async (_, __) => await ExitApplicationAsync();
        }

        private async System.Threading.Tasks.Task StartTrackingAsync()
        {
            await _trackerService.StartTrackingAsync(_appCts.Token);

            var goalTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
            goalTimer.Tick += async (_, __) => await _categoryService.CheckGoalsAsync(profileId: 1);
            goalTimer.Start();
        }

        private async System.Threading.Tasks.Task OnFormClosingAsync(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private bool _isExiting;

        private async System.Threading.Tasks.Task ExitApplicationAsync()
        {
            _isExiting = true;
            _appCts.Cancel();
            await _trackerService.StopTrackingAsync();
            _trayIconManager.Dispose();
            System.Windows.Forms.Application.Exit();
        }
    }

    /// <summary>Minimal dark renderer so the File menu matches the header bar instead of default gray.</summary>
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Theme.Accent;
        public override Color MenuItemSelectedGradientBegin => Theme.Accent;
        public override Color MenuItemSelectedGradientEnd => Theme.Accent;
        public override Color MenuItemBorder => Theme.Accent;
        public override Color MenuBorder => Theme.PrimaryDark;
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
    }
}
