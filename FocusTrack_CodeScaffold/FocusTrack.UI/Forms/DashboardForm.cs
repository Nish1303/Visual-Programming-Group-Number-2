using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FocusTrack.Business.Interfaces;

namespace FocusTrack.UI.Forms
{
    /// <summary>
    /// Shows today's tracked time per category as a pie chart plus a totals list.
    /// Refreshes automatically both on a timer and whenever the tracker records a session.
    /// </summary>
    public partial class DashboardForm : UserControl
    {
        private readonly IReportService _reportService;
        private readonly IWindowTrackerService _trackerService;

        private readonly Panel _chartCard = Theme.CreateCard(20);
        private readonly Panel _listCard = Theme.CreateCard(20);
        private readonly Chart _chart = new() { Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(80, 80) };
        private readonly ListView _totalsList = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };

        public DashboardForm(IReportService reportService, IWindowTrackerService trackerService)
        {
            _reportService = reportService;
            _trackerService = trackerService;

            BackColor = Theme.PageBackground;
            Padding = new Padding(20);

            BuildLayout();
            SetupChart();
            SetupTotalsList();

            var refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            refreshTimer.Tick += async (_, __) => await RefreshAsync();
            refreshTimer.Start();

            // Cross-thread safe: SessionRecorded fires on a background Task, so we marshal
            // back onto the UI thread with Invoke before touching any control.
            _trackerService.SessionRecorded += (_, __) =>
            {
                if (IsHandleCreated)
                    Invoke(new Action(async () => await RefreshAsync()));
            };

            Load += async (_, __) => await RefreshAsync();
        }

        private void BuildLayout()
        {
            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            _chartCard.Controls.Add(_chart);
            var chartTitle = Theme.CreateSectionTitle("Time Distribution — Today");
            WrapWithTitle(_chartCard, chartTitle, _chart);

            _listCard.Controls.Add(_totalsList);
            var listTitle = Theme.CreateSectionTitle("Category Totals");
            WrapWithTitle(_listCard, listTitle, _totalsList);

            var chartOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
            chartOuter.Controls.Add(_chartCard);
            var listOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 0, 0) };
            listOuter.Controls.Add(_listCard);

            _chartCard.Dock = DockStyle.Fill;
            _listCard.Dock = DockStyle.Fill;

            split.Controls.Add(chartOuter, 0, 0);
            split.Controls.Add(listOuter, 1, 0);

            Controls.Add(split);
        }

        /// <summary>Puts a bold section title above a content control inside a card panel.</summary>
        private static void WrapWithTitle(Panel card, Label title, Control content)
        {
            card.Controls.Clear();
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Controls.Add(title, 0, 0);
            content.Dock = DockStyle.Fill;
            layout.Controls.Add(content, 0, 1);
            card.Controls.Add(layout);
        }

        private void SetupChart()
        {
            _chart.BackColor = Theme.CardBackground;
            var area = new ChartArea("Main") { BackColor = Theme.CardBackground };
            _chart.ChartAreas.Add(area);
            _chart.Legends.Add(new Legend("Legend") { Docking = Docking.Bottom, Font = Theme.FontSmall });
            var series = new Series("Today")
            {
                ChartType = SeriesChartType.Doughnut,
                IsValueShownAsLabel = true,
                Font = Theme.FontSmall,
                LabelForeColor = Theme.TextPrimary,
            };
            _chart.Series.Add(series);
        }

        private void SetupTotalsList()
        {
            _totalsList.Columns.Add("Category", 160);
            _totalsList.Columns.Add("Minutes today", 110);
            _totalsList.Columns.Add("Goal (min)", 100);
            _totalsList.Columns.Add("Status", 160);
            Theme.StyleListView(_totalsList);
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            var today = DateTime.Today;
            var totals = await _reportService.GetChartReportAsync(profileId: 1, today, today.AddDays(1).AddSeconds(-1));

            _totalsList.Items.Clear();

            foreach (var t in totals.OrderByDescending(x => x.TotalMinutes))
            {
                string status = t.GoalMinutes > 0
                    ? (t.TotalMinutes >= t.GoalMinutes ? "\u26A0 Goal exceeded" : $"{t.GoalMinutes - t.TotalMinutes} min remaining")
                    : "No goal set";

                var item = new ListViewItem(new[] { t.CategoryName, t.TotalMinutes.ToString(), t.GoalMinutes.ToString(), status });
                if (t.GoalMinutes > 0 && t.TotalMinutes >= t.GoalMinutes)
                    item.ForeColor = Theme.Warning;
                _totalsList.Items.Add(item);
            }

            if (totals.Count == 0)
            {
                var placeholder = new ListViewItem(new[] { "No activity tracked yet today", "", "", "" });
                placeholder.ForeColor = Theme.TextMuted;
                _totalsList.Items.Add(placeholder);
            }

            // The WinForms Chart control (System.Windows.Forms.DataVisualization.Charting) can
            // throw "Height must be greater than 0px" if it's populated/redrawn before the WinForms
            // layout engine has given its container real pixel dimensions — this reliably happens
            // on the very first Load-triggered refresh, before the host window has finished its
            // first layout pass. Skipping (and letting the next 5s timer tick populate it once the
            // control has a real size) is the standard workaround; nothing is lost since the totals
            // list above — which doesn't have this limitation — already shows current data immediately.
            if (_chart.Width <= 0 || _chart.Height <= 0 || !_chart.IsHandleCreated)
                return;

            try
            {
                _chart.Series["Today"].Points.Clear();
                foreach (var t in totals.OrderByDescending(x => x.TotalMinutes))
                {
                    var point = _chart.Series["Today"].Points.Add(t.TotalMinutes);
                    point.LegendText = t.CategoryName;
                    try { point.Color = ColorTranslator.FromHtml(t.ColorHex); } catch { point.Color = Theme.Accent; }
                }
            }
            catch (ArgumentException)
            {
                // Chart still wasn't ready this cycle — safe to ignore, next timer tick retries.
            }
        }
    }
}
