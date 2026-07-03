using System.Drawing;
using System.Windows.Forms;

namespace FocusTrack.UI
{
    /// <summary>
    /// Central colour palette and small styling helpers so every view looks consistent
    /// instead of relying on default WinForms gray. Kept as plain static values —
    /// no external theming library required.
    /// </summary>
    internal static class Theme
    {
        public static readonly Color Primary = ColorTranslator.FromHtml("#2B5797");      // brand navy
        public static readonly Color PrimaryDark = ColorTranslator.FromHtml("#1E3D6B");
        public static readonly Color Accent = ColorTranslator.FromHtml("#3D8BFD");       // links / active states
        public static readonly Color Success = ColorTranslator.FromHtml("#2E8B57");
        public static readonly Color Warning = ColorTranslator.FromHtml("#C0392B");
        public static readonly Color PageBackground = ColorTranslator.FromHtml("#F4F6FA");
        public static readonly Color CardBackground = Color.White;
        public static readonly Color Border = ColorTranslator.FromHtml("#DDE3ED");
        public static readonly Color TextPrimary = ColorTranslator.FromHtml("#1F2933");
        public static readonly Color TextMuted = ColorTranslator.FromHtml("#6B7684");
        public static readonly Color RowAlt = ColorTranslator.FromHtml("#F0F3F9");

        public static readonly Font FontBase = new("Segoe UI", 9.5f);
        public static readonly Font FontHeading = new("Segoe UI", 14f, FontStyle.Bold);
        public static readonly Font FontSubheading = new("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FontSmall = new("Segoe UI", 8.5f);

        /// <summary>Applies the standard card look: white background, thin border, padding.</summary>
        public static Panel CreateCard(int padding = 16)
        {
            var panel = new Panel
            {
                BackColor = CardBackground,
                Padding = new Padding(padding),
                Margin = new Padding(0, 0, 0, 16),
            };
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };
            return panel;
        }

        public static Label CreateSectionTitle(string text) => new()
        {
            Text = text,
            Font = FontSubheading,
            ForeColor = TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };

        public static Button CreatePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Primary,
                ForeColor = Color.White,
                Font = FontBase,
                Height = 32,
                Padding = new Padding(10, 0, 10, 0),
                Cursor = Cursors.Hand,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = PrimaryDark;
            return btn;
        }

        /// <summary>Consistent header styling + alternating-row colours for any DataGridView.</summary>
        public static void StyleGrid(DataGridView grid)
        {
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = CardBackground;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = FontSubheading;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6);
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.DefaultCellStyle.SelectionBackColor = Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Padding = new Padding(4);
            grid.DefaultCellStyle.Font = FontBase;
            grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlt;
            grid.RowTemplate.Height = 28;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        }

        public static void StyleListView(ListView list)
        {
            list.BorderStyle = BorderStyle.None;
            list.Font = FontBase;
            list.GridLines = false;
            list.OwnerDraw = true;
            list.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Primary), e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header!.Text, FontSubheading, e.Bounds, Color.White,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
            // Let the framework draw item rows/subitems normally — only the header is custom-painted.
            list.DrawItem += (s, e) => e.DrawDefault = true;
            list.DrawSubItem += (s, e) => e.DrawDefault = true;
        }
    }
}
