using System;
using System.Linq;
using System.Windows.Forms;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Entities;

namespace FocusTrack.UI.Forms
{
    /// <summary>Manage categories, per-app classification / ignore list, and daily goals.</summary>
    public partial class SettingsForm : UserControl
    {
        private readonly ICategoryService _categoryService;

        private readonly ListBox _categoryList = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = Theme.FontBase, IntegralHeight = false };
        private readonly NumericUpDown _goalMinutes = new() { Minimum = 0, Maximum = 1440, Width = 90, Font = Theme.FontBase };
        private readonly Button _saveGoalButton = Theme.CreatePrimaryButton("Save goal");
        private readonly Button _addCategoryButton = Theme.CreatePrimaryButton("Add category");
        private readonly TextBox _newCategoryBox = new() { Width = 200, PlaceholderText = "New category name", Font = Theme.FontBase };

        public SettingsForm(ICategoryService categoryService)
        {
            _categoryService = categoryService;
            BackColor = Theme.PageBackground;
            Padding = new Padding(20);

            BuildLayout();
            Load += async (_, __) => await LoadCategoriesAsync();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var listCard = Theme.CreateCard(16);
            listCard.Dock = DockStyle.Fill;
            listCard.Margin = new Padding(0, 0, 16, 0);
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            listLayout.Controls.Add(Theme.CreateSectionTitle("Categories"), 0, 0);
            listLayout.Controls.Add(_categoryList, 0, 1);
            listCard.Controls.Add(listLayout);

            var detailCard = Theme.CreateCard(20);
            detailCard.Dock = DockStyle.Fill;
            var right = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };

            right.Controls.Add(Theme.CreateSectionTitle("Daily Goal"));
            var goalPanel = new FlowLayoutPanel { AutoSize = true };
            goalPanel.Controls.Add(new Label { Text = "Minutes:", Font = Theme.FontBase, ForeColor = Theme.TextPrimary, AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
            goalPanel.Controls.Add(_goalMinutes);
            var saveWrap = new Panel { AutoSize = true, Margin = new Padding(10, 0, 0, 0) };
            saveWrap.Controls.Add(_saveGoalButton);
            goalPanel.Controls.Add(saveWrap);
            right.Controls.Add(goalPanel);

            var addTitle = Theme.CreateSectionTitle("New Category");
            addTitle.Margin = new Padding(0, 28, 0, 10);
            right.Controls.Add(addTitle);
            var addPanel = new FlowLayoutPanel { AutoSize = true };
            addPanel.Controls.Add(_newCategoryBox);
            var addBtnWrap = new Panel { AutoSize = true, Margin = new Padding(10, 0, 0, 0) };
            addBtnWrap.Controls.Add(_addCategoryButton);
            addPanel.Controls.Add(addBtnWrap);
            right.Controls.Add(addPanel);

            var noteLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 28, 0, 0),
                MaximumSize = new System.Drawing.Size(480, 0),
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall,
                Text = "Ignore-list and per-application classification are managed from the " +
                       "History Browser context menu (right-click a session \u2192 Classify / Ignore)."
            };
            right.Controls.Add(noteLabel);

            detailCard.Controls.Add(right);

            root.Controls.Add(listCard, 0, 0);
            root.Controls.Add(detailCard, 1, 0);
            Controls.Add(root);

            _saveGoalButton.Click += async (_, __) => await SaveGoalAsync();
            _addCategoryButton.Click += async (_, __) => await AddCategoryAsync();
            _categoryList.SelectedIndexChanged += (_, __) => LoadSelectedGoal();
        }

        private System.Collections.Generic.List<Category> _categories = new();

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            _categories = await _categoryService.GetCategoriesAsync();
            _categoryList.Items.Clear();
            foreach (var c in _categories) _categoryList.Items.Add(c.Name);
            if (_categoryList.Items.Count > 0) _categoryList.SelectedIndex = 0;
        }

        private void LoadSelectedGoal()
        {
            var cat = SelectedCategory();
            if (cat != null) _goalMinutes.Value = Math.Min(cat.DailyGoalMinutes, (int)_goalMinutes.Maximum);
        }

        private Category? SelectedCategory()
        {
            if (_categoryList.SelectedIndex < 0) return null;
            var name = (string)_categoryList.Items[_categoryList.SelectedIndex];
            return _categories.FirstOrDefault(c => c.Name == name);
        }

        private async System.Threading.Tasks.Task SaveGoalAsync()
        {
            var cat = SelectedCategory();
            if (cat == null) return;

            try
            {
                await _categoryService.SetDailyGoalAsync(cat.Id, (int)_goalMinutes.Value);
                MessageBox.Show(this, $"Goal for \"{cat.Name}\" saved.", "FocusTrack", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not save goal: {ex.Message}", "FocusTrack", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task AddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(_newCategoryBox.Text)) return;

            try
            {
                // Persistence always goes through ICategoryService -> ICategoryRepository
                // (Data layer). This form never references FocusTrack.Data or EF Core directly.
                await _categoryService.AddCategoryAsync(_newCategoryBox.Text.Trim(), "#607D8B");
                _newCategoryBox.Clear();
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not add category: {ex.Message}", "FocusTrack", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
