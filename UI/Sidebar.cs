using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    /// <summary>
    /// The left column (.sidebar): "PS4 SAVE EDITOR" eyebrow, the "Shape your destiny"
    /// heading, the parchment chronicle card showing the loaded save, the
    /// "Keep the original safe" note and the OFFLINE TOOL / version footer.
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class Sidebar : PaintedControl
    {
        private string? _fileName;
        private long _fileSize;

        [Category("Data")]
        [Description("Name of the loaded save file, or empty when nothing is loaded.")]
        [DefaultValue(null)]
        public string? FileName
        {
            get => _fileName;
            set { _fileName = string.IsNullOrEmpty(value) ? null : value; Invalidate(); }
        }

        [Category("Data")]
        [Description("Size in bytes of the loaded save file.")]
        [DefaultValue(0L)]
        public long FileSize
        {
            get => _fileSize;
            set { _fileSize = value; Invalidate(); }
        }

        [Browsable(false)]
        public bool HasFile => _fileName != null;

        /// <summary>
        /// Left gutter in CSS px between the control's edge and the sidebar column. The
        /// control spans the page padding too, so the chronicle card's soft shadow isn't
        /// clipped on its left side. MainForm sets this to the page padding.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float ContentInset { get; set; } = 51.2f;

        [Category("Appearance")] [DefaultValue("PS4 SAVE EDITOR")]
        public string Eyebrow { get; set; } = "PS4 SAVE EDITOR";

        [Category("Appearance")] [DefaultValue("Shape your destiny")]
        public string Heading { get; set; } = "Shape your destiny";

        [Category("Appearance")]
        [DefaultValue("Prepare your Slayer for the ages of Toukiden 2. Every change remains local to your device.")]
        public string Body { get; set; } = "Prepare your Slayer for the ages of Toukiden 2. Every change remains local to your device.";

        [Category("Appearance")] [DefaultValue("Keep the original safe")]
        public string NoteTitle { get; set; } = "Keep the original safe";

        [Category("Appearance")] [DefaultValue("Export to a new file and retain an untouched backup.")]
        public string NoteBody { get; set; } = "Export to a new file and retain an untouched backup.";

        [Category("Appearance")] [DefaultValue("OFFLINE TOOL")]
        public string FooterLeft { get; set; } = "OFFLINE TOOL";

        [Category("Appearance")] [DefaultValue("v2.0")]
        public string FooterRight { get; set; } = "v2.0";

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            PaintBackdrop(g);
            Gfx.Prepare(g);

            float k = K;
            float vw = ViewportWidth, vh = ViewportHeight;

            // padding: clamp(42px,6vh,68px) clamp(28px,3vw,52px) 27px 0
            float padTop = Gfx.Clamp(42f, 0.06f * vh, 68f);
            float padRight = Gfx.Clamp(28f, 0.03f * vw, 52f);
            g.TranslateTransform(ContentInset * k, 0);
            float contentW = Width / k - ContentInset - padRight;
            float textMax = Math.Min(300f, contentW);
            float y = padTop;

            // ---- .eyebrow
            y += DrawEyebrow(g, Eyebrow, Palette.EyebrowDark, 0, y, k) + 15f;

            // ---- h1: Shippori 500, clamp(27px,3vw,39px), line-height 1.25, .06em
            float h1Size = Gfx.Clamp(27f, 0.03f * vw, 39f);
            var h1 = FontLibrary.Get(Face.Mincho500, h1Size * k);
            float h1Track = 0.06f * h1Size * k;
            float h1Lh = 1.25f * h1Size * Ui.TextScale;
            foreach (var line in Gfx.Wrap(Heading, h1, h1Track, textMax * k))
            {
                Gfx.DrawText(g, line, h1, Palette.HeadingLight, 0, y * k, h1Lh * k, h1Track);
                y += h1Lh;
            }

            // ---- paragraph: 12px / 1.9, margin-top 16
            y += 16f;
            var body = FontLibrary.Get(Face.Serif500, 12f * k);
            foreach (var line in Gfx.Wrap(Body, body, 0, textMax * k))
            {
                Gfx.DrawText(g, line, body, Palette.BodyMuted, 0, y * k, 22.8f * Ui.TextScale * k);
                y += 22.8f * Ui.TextScale;
            }

            // ---- .save-card, margin-top clamp(30px,5vh,48px), min-height 88px
            y += Gfx.Clamp(30f, 0.05f * vh, 48f);
            float cardH = 88f;
            DrawSaveCard(g, new RectangleF(0, y * k, contentW * k, cardH * k), k);
            y += cardH;

            // ---- .sidebar-rule: margin 31px 0, gradient hairline
            y += 31f;
            using (var rule = new LinearGradientBrush(
                new RectangleF(0, y * k, Math.Max(1f, contentW * k), Math.Max(1f, k)),
                Gfx.Rgba(230, 220, 196, 0.2), Gfx.Rgba(230, 220, 196, 0), LinearGradientMode.Horizontal))
            {
                g.FillRectangle(rule, 0, y * k, contentW * k, Math.Max(1f, k));
            }
            y += 1f + 31f;

            // ---- .warding-note
            DrawWardingNote(g, y, contentW, k);

            // ---- .sidebar-footer (pinned to the bottom, padding-bottom 27px)
            var foot = FontLibrary.Get(Face.Serif500, 8f * k);
            float footLh = Gfx.NormalLineHeight(foot);
            float footTop = Height - 27f * k - footLh;
            float footTrack = 0.22f * 8f * k;
            Gfx.DrawText(g, FooterLeft, foot, Palette.FooterText, 0, footTop, footLh, footTrack);
            float rw = Gfx.Measure(FooterRight, foot, footTrack);
            Gfx.DrawText(g, FooterRight, foot, Palette.FooterText, contentW * k - rw, footTop, footLh, footTrack);
        }

        /// <summary>Draws a .eyebrow (22px vermilion rule + tracked caps). Returns its height in CSS px.</summary>
        internal static float DrawEyebrow(Graphics g, string text, Color color, float x, float y, float k)
        {
            var font = FontLibrary.Get(Face.Serif600, 9f * k);
            float lh = Gfx.NormalLineHeight(font);
            float midY = y * k + lh / 2f;
            using (var b = new SolidBrush(Palette.Vermilion))
            {
                g.FillRectangle(b, x * k, midY - 0.5f * k, 22f * k, Math.Max(1f, k));
            }
            Gfx.DrawText(g, text, font, color, (x + 33f) * k, y * k, lh, 0.3f * 9f * k);
            return lh / k;
        }

        private void DrawSaveCard(Graphics g, RectangleF card, float k)
        {
            // box-shadow: 0 10px 30px rgba(0,0,0,.2)
            Gfx.DropShadow(g, card, 10f * k, 30f * k, Gfx.Rgba(0, 0, 0, 0.2));

            // linear-gradient(rgba(226,215,191,.96), rgba(210,197,169,.96))
            using (var bg = new LinearGradientBrush(card, Gfx.Rgba(226, 215, 191, 0.96), Gfx.Rgba(210, 197, 169, 0.96), LinearGradientMode.Vertical))
            {
                g.FillRectangle(bg, card);
            }

            // ::before inner frame, inset 5px
            Gfx.Border(g, Gfx.Rgba(75, 65, 49, 0.26), RectangleF.Inflate(card, -5f * k, -5f * k), Math.Max(1f, k));

            // ::after vermilion strip once a file is loaded
            if (HasFile)
            {
                using var strip = new SolidBrush(Palette.Vermilion);
                g.FillRectangle(strip, card.X + 5f * k, card.Y + 5f * k, 3f * k, card.Height - 10f * k);
            }

            // .save-card-icon: 51px frame, 殺
            var icon = new RectangleF(card.X + 17f * k, card.Y + (card.Height - 51f * k) / 2f, 51f * k, 51f * k);
            Gfx.Border(g, Gfx.Rgba(75, 65, 49, 0.28), icon, Math.Max(1f, k));
            var glyph = FontLibrary.Get(Face.Mincho700, 26f * k, decorative: true);
            float gcx = icon.X + icon.Width / 2f;
            float gTop = icon.Y + (icon.Height - 26f * k) / 2f;
            Gfx.DrawTextCentered(g, "殺", glyph, Gfx.Rgba(166, 59, 43, 0.45), gcx + 1f * k, gTop + 1f * k, 26f * k);
            Gfx.DrawTextCentered(g, "殺", glyph, Gfx.Rgba(255, 248, 225, 0.35), gcx - 1f * k, gTop - 1f * k, 26f * k);
            Gfx.DrawTextCentered(g, "殺", glyph, Palette.CardGlyph, gcx, gTop, 26f * k);

            // .save-card-copy
            var small = FontLibrary.Get(Face.Serif500, 8f * k);
            var strong = FontLibrary.Get(Face.Serif600, 12f * k);
            float smallLh = Gfx.NormalLineHeight(small);
            float strongLh = Gfx.NormalLineHeight(strong);
            float copyH = smallLh + 6f * k + strongLh + 6f * k + smallLh;
            float x = icon.Right + 14f * k;
            float maxW = card.Right - 15f * k - x;
            float cy = card.Y + (card.Height - copyH) / 2f;

            string label = HasFile ? "ACTIVE CHRONICLE" : "CHRONICLE STATUS";
            string name = _fileName ?? "No save selected";
            string meta = HasFile
                ? $"{_fileSize / 1024.0:0.0} KB · Local file"
                : "Import a PS4 save to begin";

            Gfx.DrawText(g, label, small, Palette.CardLabel, x, cy, smallLh, 0.15f * 8f * k);
            cy += smallLh + 6f * k;
            Gfx.DrawText(g, Gfx.Ellipsize(name, strong, 0, maxW), strong, Palette.Ink, x, cy, strongLh);
            cy += strongLh + 6f * k;
            Gfx.DrawText(g, Gfx.Ellipsize(meta, small, 0, maxW), small, Palette.CardLabel, x, cy, smallLh);
        }

        private void DrawWardingNote(Graphics g, float y, float contentW, float k)
        {
            // .warding-symbol: 36 x 48, vermilion-tinted box with 守
            var box = new RectangleF(0, y * k, 36f * k, 48f * k);
            using (var fill = new SolidBrush(Gfx.Rgba(166, 59, 43, 0.12)))
            {
                g.FillRectangle(fill, box);
            }
            Gfx.Border(g, Gfx.Rgba(166, 59, 43, 0.48), box, Math.Max(1f, k));
            var symbol = FontLibrary.Get(Face.Serif500, 17f * k, decorative: true);
            float symLh = Gfx.NormalLineHeight(symbol);
            Gfx.DrawTextCentered(g, "守", symbol, Palette.WardGlyph, box.X + box.Width / 2f, box.Y + (box.Height - symLh) / 2f, symLh);

            // copy
            float x = (36f + 14f) * k;
            var title = FontLibrary.Get(Face.Serif500, 11f * k);
            float titleLh = Gfx.NormalLineHeight(title);
            Gfx.DrawText(g, NoteTitle, title, Palette.WardTitle, x, y * k, titleLh, 0.05f * 11f * k);

            var body = FontLibrary.Get(Face.Serif500, 10f * k);
            float lh = 17f * Ui.TextScale * k; // 10px * 1.7
            float ty = y * k + titleLh + 5f * k;
            foreach (var line in Gfx.Wrap(NoteBody, body, 0, contentW * k - x))
            {
                Gfx.DrawText(g, line, body, Palette.WardBody, x, ty, lh);
                ty += lh;
            }
        }
    }
}
