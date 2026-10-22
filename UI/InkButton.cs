using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    public enum InkButtonVariant
    {
        /// <summary>Vermilion with cream text (Import save).</summary>
        Vermilion,
        /// <summary>Teal with white text (Export save - the Make file's inline style).</summary>
        Teal,
    }

    public enum InkButtonIcon
    {
        None,
        Upload,
        Download,
    }

    /// <summary>
    /// The square action buttons in the bottom bar (.import-button / .export-button):
    /// solid fill, a 1px inner frame inset 3px, a line icon and tracked uppercase
    /// label. When disabled it renders the CSS "grayscale(1) + opacity .28" look.
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    [DefaultEvent("Click")]
    public class InkButton : PaintedControl
    {
        private InkButtonVariant _variant;
        private InkButtonIcon _icon = InkButtonIcon.Upload;
        private bool _hover;
        private bool _pressed;

        public InkButton()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        [Category("Appearance")]
        [DefaultValue(InkButtonVariant.Vermilion)]
        public InkButtonVariant Variant
        {
            get => _variant;
            set { _variant = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(InkButtonIcon.Upload)]
        public InkButtonIcon Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        /// <summary>Colour painted behind the button (the action bar's background).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Underlay { get; set; } = Palette.ActionBarBack;

        /// <summary>Natural width (padding 0 19px + icon + gap + label) in device px.</summary>
        public int PreferredButtonWidth()
        {
            float k = K;
            var font = LabelFont(k);
            float textW = Gfx.Measure(LabelText, font, 0.08f * 10f * k);
            float iconW = _icon == InkButtonIcon.None ? 0f : 15f * k + 10f * k;
            return (int)Math.Ceiling(19f * k + iconW + textW + 19f * k);
        }

        public int PreferredButtonHeight() => (int)Math.Ceiling(41f * K);

        private string LabelText => (Text ?? string.Empty).ToUpperInvariant();

        private static Font LabelFont(float k) => FontLibrary.Get(Face.Serif600, 10f * k);

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            Parent?.PerformLayout();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.No;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            PaintBackdrop(g);
            using (var under = new SolidBrush(Underlay))
            {
                g.FillRectangle(under, 0, 0, Width, Height);
            }

            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            if (Enabled)
            {
                Gfx.Prepare(g);
                DrawFace(g);
                return;
            }

            // .export-button:disabled { filter: grayscale(1); opacity: .28 }
            using var face = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (var fg = Graphics.FromImage(face))
            {
                Gfx.Prepare(fg);
                // ClearType needs an opaque surface; this scratch bitmap is transparent.
                fg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                fg.Clear(Color.Transparent);
                DrawFace(fg);
            }

            var matrix = new ColorMatrix(new[]
            {
                new[] { 0.2126f, 0.2126f, 0.2126f, 0f, 0f },
                new[] { 0.7152f, 0.7152f, 0.7152f, 0f, 0f },
                new[] { 0.0722f, 0.0722f, 0.0722f, 0f, 0f },
                new[] { 0f, 0f, 0f, 0.28f, 0f },
                new[] { 0f, 0f, 0f, 0f, 1f },
            });
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);
            g.DrawImage(face, new Rectangle(0, 0, Width, Height), 0, 0, Width, Height, GraphicsUnit.Pixel, attributes);
        }

        private void DrawFace(Graphics g)
        {
            float k = K;
            Color back, fore, frame;
            if (_variant == InkButtonVariant.Teal)
            {
                back = Palette.ExportBack;              // inline style wins over :hover in the design
                fore = Palette.ExportText;
                frame = Gfx.Rgba(50, 43, 32, 0.2);
            }
            else
            {
                back = _hover && Enabled ? Palette.VermilionBright : Palette.Vermilion;
                fore = Palette.ImportText;
                frame = Gfx.Rgba(238, 229, 209, 0.2);
            }

            if (_pressed && Enabled)
            {
                back = ControlPaint.Dark(back, 0.05f);
            }

            var rect = new RectangleF(0, 0, Width, Height);
            using (var b = new SolidBrush(back))
            {
                g.FillRectangle(b, rect);
            }
            Gfx.Border(g, frame, RectangleF.Inflate(rect, -3f * k, -3f * k), Math.Max(1f, k));

            if (Focused && ShowFocusCues)
            {
                using var focus = new Pen(Gfx.WithAlpha(fore, 0.6), Math.Max(1f, k)) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(focus, 5f * k, 5f * k, Width - 10f * k - 1, Height - 10f * k - 1);
            }

            // content: [icon 15px] gap 10px [LABEL], centred
            var font = LabelFont(k);
            float tracking = 0.08f * 10f * k;
            string label = LabelText;
            float textW = Gfx.Measure(label, font, tracking);
            bool hasIcon = _icon != InkButtonIcon.None;
            float total = (hasIcon ? 15f * k + 10f * k : 0f) + textW;
            float x = (Width - total) / 2f;
            float midY = Height / 2f;

            if (hasIcon)
            {
                DrawIcon(g, _icon, x, midY - 7.5f * k, 15f * k, fore);
                x += 25f * k;
            }

            float lh = Gfx.NormalLineHeight(font);
            Gfx.DrawText(g, label, font, fore, x, midY - lh / 2f, lh, tracking);
        }

        private static void DrawIcon(Graphics g, InkButtonIcon icon, float x, float y, float size, Color color)
        {
            float s = size / 24f;
            PointF P(float px, float py) => new(x + px * s, y + py * s);

            using var path = new GraphicsPath();
            if (icon == InkButtonIcon.Upload)
            {
                // M12 16V3 m0 0L7 8 m5-5 5 5 M5 14v5a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-5
                path.StartFigure(); path.AddLine(P(12, 16), P(12, 3));
                path.StartFigure(); path.AddLines(new[] { P(7, 8), P(12, 3), P(17, 8) });
                path.StartFigure();
                path.AddLine(P(5, 14), P(5, 19));
                path.AddArc(x + 5 * s, y + 17 * s, 4 * s, 4 * s, 180f, -90f);
                path.AddLine(P(7, 21), P(17, 21));
                path.AddArc(x + 15 * s, y + 17 * s, 4 * s, 4 * s, 90f, -90f);
                path.AddLine(P(19, 19), P(19, 14));
            }
            else if (icon == InkButtonIcon.Download)
            {
                // M12 3v13 m0 0 5-5 m-5 5-5-5 M5 19v2h14v-2
                path.StartFigure(); path.AddLine(P(12, 3), P(12, 16));
                path.StartFigure(); path.AddLines(new[] { P(17, 11), P(12, 16), P(7, 11) });
                path.StartFigure(); path.AddLines(new[] { P(5, 19), P(5, 21), P(19, 21), P(19, 19) });
            }

            using var pen = new Pen(color, 1.7f * s)
            {
                StartCap = LineCap.Square,
                EndCap = LineCap.Square,
                LineJoin = LineJoin.Miter,
            };
            g.DrawPath(pen, path);
        }
    }
}
