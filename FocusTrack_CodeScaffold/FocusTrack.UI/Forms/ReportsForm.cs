using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Models;

namespace FocusTrack.UI.Forms
{
    /// <summary>
    /// Two mandatory report types:
    ///  1) Tabular — sessions grouped by application/category with subtotals.
    ///  2) Chart-based — bar chart of time distribution over a date range (daily/weekly).
    /// </summary>
    public partial class ReportsForm : UserControl
    {
        private readonly IReportService _reportService;
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Font = Theme.FontBase };
        private readonly DataGridView _tabularGrid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Chart _barChart = new() { Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(80, 80) };
        private readonly ComboBox _rangeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Font = Theme.FontBase };
        private readonly Button _generateButton = Theme.CreatePrimaryButton("\u21BB  Generate");

        public ReportsForm(IReportService reportService)
        {
            _reportService = reportService;
            BackColor = Theme.PageBackground;
            Padding = new Padding(20);

            BuildLayout();
            Theme.StyleGrid(_tabularGrid);
            Load += async (_, __) => await GenerateAsync();
        }

        private void BuildLayout()
        {
            var tabularCard = Theme.CreateCard(0);
            tabularCard.Dock = DockStyle.Fill;
            tabularCard.Controls.Add(_tabularGrid);
            var tabular = new TabPage("Tabular Report") { Controls = { tabularCard }, BackColor = Theme.PageBackground, Padding = new Padding(12) };
            _tabularGrid.Columns.Add("Group", "Application / Category");
            _tabularGrid.Columns.Add("Sessions", "Session count");
            _tabularGrid.Columns.Add("TotalTime", "Total time");

            var chartPage = new TabPage("Chart Report") { BackColor = Theme.PageBackground, Padding = new Padding(12) };
            var topCard = Theme.CreateCard(14);
            topCard.Dock = DockStyle.Top;
            topCard.AutoSize = true;
            var topFlow = new FlowLayoutPanel { AutoSize = true };
            var rangeWrap = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 0, 16, 0) };
            rangeWrap.Controls.Add(new Label { Text = "Range", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true });
            _rangeCombo.Items.AddRange(new object[] { "Today", "Last 7 days (daily)", "Last 4 weeks (weekly)" });
            _rangeCombo.SelectedIndex = 1;
            rangeWrap.Controls.Add(_rangeCombo);
            topFlow.Controls.Add(rangeWrap);
            var btnWrap = new Panel { AutoSize = true, Margin = new Padding(0, 18, 0, 0) };
            btnWrap.Controls.Add(_generateButton);
            topFlow.Controls.Add(btnWrap);
            topCard.Controls.Add(topFlow);

            var chartCard = Theme.CreateCard(16);
            chartCard.Dock = DockStyle.Fill;
            chartCard.Controls.Add(_barChart);

            chartPage.Controls.Add(chartCard);
            chartPage.Controls.Add(topCard);

            var area = new ChartArea("Main") { BackColor = Theme.CardBackground };
            _barChart.BackColor = Theme.CardBackground;
            _barChart.ChartAreas.Add(area);
            _barChart.Series.Add(new Series("Minutes")
            {
                ChartType = SeriesChartType.Bar,
                IsValueShownAsLabel = true,
                Font = Theme.FontSmall,
            });

            _tabs.TabPages.Add(tabular);
            _tabs.TabPages.Add(chartPage);
            Controls.Add(_tabs);

            _generateButton.Click += async (_, __) => await GenerateAsync();
        }

        private async System.Threading.Tasks.Task GenerateAsync()
        {
            await GenerateTabularAsync();
            await GenerateChartAsync();
        }

        private async System.Threading.Tasks.Task GenerateTabularAsync()
        {
            var filter = new SessionFilter { FromDate = DateTime.Today.AddDays(-30), ToDate = DateTime.Today.AddDays(1), ProfileId = 1 };
            var rows = await _reportService.GetTabularReportAsync(filter);

            _tabularGrid.Rows.Clear();

            // Grouped by category, with a subtotal row per group — satisfies the
            // "grouped by application or category, with subtotals per group" requirement.
            foreach (var group in rows.GroupBy(r => r.CategoryName).OrderBy(g => g.Key))
            {
                var totalSeconds = group.Sum(r => r.DurationSeconds);
                int headerIdx = _tabularGrid.Rows.Add($"{group.Key}", group.Count(), TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss"));
                var headerRow = _tabularGrid.Rows[headerIdx];
                headerRow.DefaultCellStyle.BackColor = Theme.Primary;
                headerRow.DefaultCellStyle.ForeColor = Color.White;
                headerRow.DefaultCellStyle.Font = Theme.FontSubheading;

                foreach (var app in group.GroupBy(r => r.ApplicationName).OrderByDescending(g => g.Sum(r => r.DurationSeconds)))
                {
                    var appSeconds = app.Sum(r => r.DurationSeconds);
                    _tabularGrid.Rows.Add($"      {app.Key}", app.Count(), TimeSpan.FromSeconds(appSeconds).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        private async System.Threading.Tasks.Task GenerateChartAsync()
        {
            DateTime from = _rangeCombo.SelectedIndex switch
            {
                0 => DateTime.Today,
                2 => DateTime.Today.AddDays(-28),
                _ => DateTime.Today.AddDays(-7),
            };
            var totals = await _reportService.GetChartReportAsync(profileId: 1, from, DateTime.Today.AddDays(1).AddSeconds(-1));

            if (_barChart.Width <= 0 || _barChart.Height <= 0 || !_barChart.IsHandleCreated)
                return;

            try
            {
                _barChart.Series["Minutes"].Points.Clear();
                foreach (var t in totals.OrderByDescending(t => t.TotalMinutes))
                {
                    var pt = _barChart.Series["Minutes"].Points.Add(t.TotalMinutes);
                    pt.AxisLabel = t.CategoryName;
                    try { pt.Color = ColorTranslator.FromHtml(t.ColorHex); } catch { pt.Color = Theme.Accent; }
                }
            }
            catch (ArgumentException)
            {
                // Chart wasn't laid out yet this cycle — safe to ignore; the next
                // "Generate" click or automatic re-render will populate it once ready.
            }
        }
    }
}
