using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    /// <summary>
    /// The 92px header: the diamond 伐 brand mark with "TOUKIDEN 2 / SLAYER'S HAVEN"
    /// on the left and the save status on the right (.topbar in the design).
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class TopBar : PaintedControl
    {
        private bool _isReady;
        private string _title = "TOUKIDEN 2";
        private string _subtitle = "SLAYER'S HAVEN";

        [Category("Appearance")]
        [Description("True once a save file is loaded - turns the status dot olive and changes the label.")]
        [DefaultValue(false)]
        public bool IsReady
        {
            get => _isReady;
            set { _isReady = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("TOUKIDEN 2")]
        public string Title
        {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("SLAYER'S HAVEN")]
        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("Save loaded")]
        public string ReadyText { get; set; } = "Save loaded";

        [Category("Appearance")]
        [DefaultValue("No save loaded")]
        public string IdleText { get; set; } = "No save loaded";

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            PaintBackdrop(g);
            Gfx.Prepare(g);

            float k = K;
            float padX = Gfx.Clamp(24f, 0.04f * ViewportWidth, 64f);
            float midY = 46f * k; // vertical centre of the 92px bar

            // ---- .brand-mark: 52px square rotated 45deg with two vermilion inner frames
            float markCx = (padX + 26f) * k;
            DrawDiamond(g, markCx, midY, 52f * k, Gfx.Rgba(212, 194, 156, 0.38), k);
            DrawDiamond(g, markCx, midY, 42f * k, Gfx.Rgba(166, 59, 43, 0.55), k);
            DrawDiamond(g, markCx, midY, 32f * k, Gfx.Rgba(166, 59, 43, 0.55), k);

            // 伐 - Shippori 700 25px, text-shadow 1px 0 vermilion, -1px 0 rgba(238,229,209,.25)
            var glyphFont = FontLibrary.Get(Face.Mincho700, 25f * k, decorative: true);
            float glyphTop = midY - 12.5f * k;
            Gfx.DrawTextCentered(g, "伐", glyphFont, Palette.Vermilion, markCx + 1f * k, glyphTop, 25f * k);
            Gfx.DrawTextCentered(g, "伐", glyphFont, Gfx.Rgba(238, 229, 209, 0.25), markCx - 1f * k, glyphTop, 25f * k);
            Gfx.DrawTextCentered(g, "伐", glyphFont, Palette.BrandGlyph, markCx, glyphTop, 25f * k);

            // ---- wordmark: strong 19px Shippori 600 / .22em, small 9px / .3em
            float textX = (padX + 52f + 17f) * k;
            var titleFont = FontLibrary.Get(Face.Mincho600, 19f * k);
            var subFont = FontLibrary.Get(Face.Serif500, 9f * k);
            float subLh = Gfx.NormalLineHeight(subFont);
            float blockH = 19f * k + 7f * k + subLh;
            float top = midY - blockH / 2f;
            Gfx.DrawText(g, _title, titleFont, Palette.BrandText, textX, top, 19f * k, 0.22f * 19f * k);
            Gfx.DrawText(g, _subtitle.ToUpperInvariant(), subFont, Palette.BrandSub, textX, top + 26f * k, subLh, 0.3f * 9f * k);

            // ---- .top-status (right aligned)
            var statusFont = FontLibrary.Get(Face.Serif500, 11f * k);
            string status = _isReady ? ReadyText : IdleText;
            float tracking = 0.05f * 11f * k;
            float statusW = Gfx.Measure(status, statusFont, tracking);
            float right = Width - padX * k;
            float textLeft = right - statusW;
            float lh = Gfx.NormalLineHeight(statusFont);
            Gfx.DrawText(g, status, statusFont, Palette.StatusText, textLeft, midY - lh / 2f, lh, tracking);
            Gfx.StatusDot(g, textLeft - 10f * k - 3f * k, midY, k, _isReady);
        }

        private static void DrawDiamond(Graphics g, float cx, float cy, float side, Color color, float k)
        {
            float stroke = Math.Max(1f, k);
            float half = (side / 2f - stroke / 2f) * (float)Math.Sqrt(2.0);
            var pts = new[]
            {
                new PointF(cx, cy - half),
                new PointF(cx + half, cy),
                new PointF(cx, cy + half),
                new PointF(cx - half, cy),
            };
            using var pen = new Pen(color, stroke);
            g.DrawPolygon(pen, pts);
        }
    }
}
