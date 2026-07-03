using System;
using System.Linq;
using System.Windows.Forms;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Models;

namespace FocusTrack.UI.Forms
{
    /// <summary>Browse past sessions with date range / application / category filters.</summary>
    public partial class HistoryForm : UserControl
    {
        private readonly IReportService _reportService;
        private readonly ICategoryService _categoryService;

        private readonly DateTimePicker _fromPicker = new() { Value = DateTime.Today.AddDays(-7), Font = Theme.FontBase, Width = 120 };
        private readonly DateTimePicker _toPicker = new() { Value = DateTime.Today, Font = Theme.FontBase, Width = 120 };
        private readonly TextBox _appNameBox = new() { Width = 170, PlaceholderText = "Application name", Font = Theme.FontBase };
        private readonly ComboBox _categoryCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Font = Theme.FontBase };
        private readonly Button _searchButton = Theme.CreatePrimaryButton("\uD83D\uDD0D  Search");
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Panel _filterCard = Theme.CreateCard(14);
        private readonly Panel _gridCard = Theme.CreateCard(0);

        public HistoryForm(IReportService reportService, ICategoryService categoryService)
        {
            _reportService = reportService;
            _categoryService = categoryService;

            BackColor = Theme.PageBackground;
            Padding = new Padding(20);

            BuildLayout();
            Theme.StyleGrid(_grid);

            _searchButton.Click += async (_, __) => await SearchAsync();
            Load += async (_, __) => { await LoadCategoriesAsync(); await SearchAsync(); };
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var filterFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            filterFlow.Controls.Add(LabeledField("From", _fromPicker));
            filterFlow.Controls.Add(LabeledField("To", _toPicker));
            filterFlow.Controls.Add(LabeledField("Application", _appNameBox));
            filterFlow.Controls.Add(LabeledField("Category", _categoryCombo));
            var btnWrap = new Panel { AutoSize = true, Margin = new Padding(4, 22, 0, 0) };
            btnWrap.Controls.Add(_searchButton);
            filterFlow.Controls.Add(btnWrap);
            _filterCard.Controls.Add(filterFlow);
            _filterCard.Dock = DockStyle.Top;
            _filterCard.AutoSize = true;
            _filterCard.Margin = new Padding(0, 0, 0, 16);

            _gridCard.Dock = DockStyle.Fill;
            _gridCard.Controls.Add(_grid);

            root.Controls.Add(_filterCard, 0, 0);
            root.Controls.Add(_gridCard, 0, 1);
            Controls.Add(root);

            _grid.Columns.Add("ApplicationName", "Application");
            _grid.Columns.Add("CategoryName", "Category");
            _grid.Columns.Add("WindowTitle", "Window Title");
            _grid.Columns.Add("StartTime", "Start");
            _grid.Columns.Add("EndTime", "End");
            _grid.Columns.Add("Duration", "Duration");
        }

        private static Control LabeledField(string labelText, Control field)
        {
            var wrap = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 0, 16, 0) };
            wrap.Controls.Add(new Label { Text = labelText, Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(2, 0, 0, 2) });
            wrap.Controls.Add(field);
            return wrap;
        }

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            _categoryCombo.Items.Clear();
            _categoryCombo.Items.Add("(All categories)");
            var categories = await _categoryService.GetCategoriesAsync();
            foreach (var c in categories) _categoryCombo.Items.Add(c.Name);
            _categoryCombo.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task SearchAsync()
        {
            var filter = new SessionFilter
            {
                FromDate = _fromPicker.Value.Date,
                ToDate = _toPicker.Value.Date.AddDays(1).AddSeconds(-1),
                ApplicationNameContains = string.IsNullOrWhiteSpace(_appNameBox.Text) ? null : _appNameBox.Text,
                ProfileId = 1
            };

            var rows = await _reportService.GetTabularReportAsync(filter);

            _grid.Rows.Clear();
            foreach (var r in rows.OrderByDescending(x => x.StartTime))
            {
                _grid.Rows.Add(
                    r.ApplicationName,
                    r.CategoryName,
                    r.WindowTitle,
                    r.StartTime.ToString("g"),
                    r.EndTime.ToString("g"),
                    TimeSpan.FromSeconds(r.DurationSeconds).ToString(@"hh\:mm\:ss"));
            }
        }
    }
}
