using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    /// <summary>
    /// Implemented by the main form: lets every child control paint the slice of the
    /// shared, pre-rendered background that sits behind it, which is how the
    /// "transparent" look of the web design is reproduced without WinForms'
    /// expensive (and flickery) real transparency.
    /// </summary>
    internal interface IBackdropHost
    {
        void PaintBackdrop(Graphics g, Control target);
    }

    /// <summary>
    /// Base class for all custom-painted pieces of the UI: double buffered, redraws on
    /// resize, and knows the current scale factor and viewport (in CSS px) so it can
    /// evaluate the design's clamp()/vw/vh rules.
    /// </summary>
    [ToolboxItem(false)]
    public class PaintedControl : Control
    {
        public PaintedControl()
        {
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Palette.Sumi;
        }

        /// <summary>Viewport width in CSS px (window client width / scale).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float ViewportWidth { get; set; } = 1280f;

        /// <summary>Viewport height in CSS px (window client height / scale).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float ViewportHeight { get; set; } = 900f;

        /// <summary>Device pixels per CSS pixel.</summary>
        protected float K => Ui.ScaleOf(this);

        public void SetViewport(float vw, float vh)
        {
            ViewportWidth = vw;
            ViewportHeight = vh;
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Everything (including the background slice) is drawn in OnPaint.
        }

        protected void PaintBackdrop(Graphics g)
        {
            Backdrop.PaintFor(g, this);
        }
    }

    internal static class Backdrop
    {
        public static void PaintFor(Graphics g, Control target)
        {
            if (target.FindForm() is IBackdropHost host)
            {
                host.PaintBackdrop(g, target);
            }
            else
            {
                // Forms Designer / no host: plain sumi.
                using var b = new SolidBrush(Palette.Sumi);
                g.FillRectangle(b, 0, 0, target.Width, target.Height);
            }
        }
    }

    /// <summary>
    /// Device-pixel rectangles of the four regions of the window, computed from the
    /// same rules as the Figma Make stylesheet (.topbar, .workspace grid, .editor
    /// margins, .action-bar).
    /// </summary>
    internal readonly struct ShellLayout
    {
        public float K { get; init; }
        public float Vw { get; init; }
        public float Vh { get; init; }
        public float PadX { get; init; }
        public Rectangle TopBar { get; init; }
        public Rectangle Sidebar { get; init; }
        public Rectangle Editor { get; init; }
        public Rectangle ActionBar { get; init; }

        public const float TopBarHeight = 92f;
        public const float ActionBarHeight = 78f;

        public static ShellLayout Compute(Size client, float k)
        {
            float vw = Math.Max(1, client.Width) / k;
            float vh = Math.Max(1, client.Height) / k;

            // padding: 0 clamp(24px, 4vw, 64px)
            float padX = Gfx.Clamp(24f, 0.04f * vw, 64f);

            // grid-template-columns: minmax(260px, 340px) minmax(0, 1fr)
            float sideW = Gfx.Clamp(260f, (vw - 2f * padX) * 0.32f, 340f);

            float workTop = TopBarHeight;
            float workBottom = vh - ActionBarHeight;

            // .editor margin: clamp(30px,4vh,44px) 0 clamp(30px,4vh,44px) clamp(30px,4vw,64px); max-width 1100px
            float edMarginY = Gfx.Clamp(30f, 0.04f * vh, 44f);
            float edLeft = padX + sideW + Gfx.Clamp(30f, 0.04f * vw, 64f);
            float edRight = Math.Min(vw - padX, edLeft + 1100f);
            float edTop = workTop + edMarginY;
            float edBottom = Math.Max(edTop + 200f, workBottom - edMarginY);

            return new ShellLayout
            {
                K = k,
                Vw = vw,
                Vh = vh,
                PadX = padX,
                // The top bar control stops 2px above its bottom border so the
                // vermilion accent (drawn in the backdrop) can straddle the line.
                TopBar = R(0, 0, vw, TopBarHeight - 2f, k),
                Sidebar = R(padX, workTop, sideW, workBottom - workTop, k),
                Editor = R(edLeft, edTop, edRight - edLeft, edBottom - edTop, k),
                ActionBar = R(0, vh - ActionBarHeight, vw, ActionBarHeight, k),
            };
        }

        private static Rectangle R(float x, float y, float w, float h, float k)
        {
            int left = (int)Math.Round(x * k);
            int top = (int)Math.Round(y * k);
            int right = (int)Math.Round((x + w) * k);
            int bottom = (int)Math.Round((y + h) * k);
            return Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));
        }
    }

    /// <summary>
    /// Renders the static background of the window once per size: the sumi base with
    /// its two radial glows, paper-grain noise, the seigaiha wave band, the giant
    /// faint 鬼, hairlines, the vertical 陰陽 and the workbench's drop shadow.
    /// </summary>
    internal static class BackdropRenderer
    {
        private static Bitmap? _noiseTile;

        public static Bitmap Render(Size client, ShellLayout m)
        {
            int w = Math.Max(1, client.Width);
            int h = Math.Max(1, client.Height);
            float k = m.K;

            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            Gfx.Prepare(g);

            // background-color: var(--sumi)
            g.Clear(Palette.Sumi);

            // radial-gradient(circle at 95% 35%, rgba(123,48,37,.13), transparent 32rem)
            RadialGlow(g, w * 0.95f, h * 0.35f, 512f * k, Gfx.Rgba(123, 48, 37, 0.13), w, h);
            // radial-gradient(circle at 18% 0%, rgba(74,82,70,.4), transparent 35rem)
            RadialGlow(g, w * 0.18f, 0f, 560f * k, Gfx.Rgba(74, 82, 70, 0.4), w, h);

            // .app-shell::before - fractal noise grain
            using (var noise = new TextureBrush(NoiseTile(), WrapMode.Tile))
            {
                g.FillRectangle(noise, 0, 0, w, h);
            }

            // .app-shell::after - seigaiha band above the action bar, 42% of the height
            Seigaiha(g, k, w, h);

            // .ambient-mark - 鬼, font-size min(50vw, 39rem), right -3vw, top 13vh
            {
                float size = Math.Min(0.5f * m.Vw, 624f) * k;
                var font = FontLibrary.Get(Face.Mincho700, size, decorative: true);
                float glyphW = Gfx.Measure("鬼", font);
                float right = (m.Vw * 1.03f) * k;
                float top = 0.13f * m.Vh * k;
                Gfx.DrawText(g, "鬼", font, Gfx.Rgba(211, 197, 166, 0.025), right - glyphW, top, size);
            }

            // .topbar border-bottom + the 46px vermilion accent (bottom -2px, 3px tall)
            Gfx.Hairline(g, Palette.Hairline, 0, (ShellLayout.TopBarHeight - 1f) * k, w, Math.Max(1f, k));
            using (var accent = new SolidBrush(Palette.Vermilion))
            {
                g.FillRectangle(accent, m.PadX * k, (ShellLayout.TopBarHeight - 2f) * k, 46f * k, 3f * k);
            }

            // .sidebar border-right
            var sb = m.Sidebar;
            using (var line = new SolidBrush(Palette.Hairline))
            {
                g.FillRectangle(line, sb.Right - Math.Max(1f, k), sb.Top, Math.Max(1f, k), sb.Height);
            }

            // .sidebar::after - "陰陽", 10px, letter-spacing .65em, rotated 90deg at right:-22px; top:50%
            {
                var font = FontLibrary.Get(Face.Serif500, 10f * k, decorative: true);
                float tracking = 6.5f * k;
                float tw = Gfx.Measure("陰陽", font, tracking);
                float lh = Gfx.NormalLineHeight(font);
                float boxRight = sb.Right + 22f * k;
                float cx = boxRight - tw / 2f;
                float cy = sb.Top + sb.Height * 0.5f + lh / 2f;
                var state = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(90f);
                Gfx.DrawText(g, "陰陽", font, Gfx.Rgba(218, 205, 178, 0.12), -tw / 2f, -lh / 2f, lh, tracking);
                g.Restore(state);
            }

            // .editor::before box-shadow: 0 22px 60px rgba(0,0,0,.3)
            Gfx.DropShadow(g, m.Editor, 22f * k, 60f * k, Gfx.Rgba(0, 0, 0, 0.3));

            return bmp;
        }

        private static void RadialGlow(Graphics g, float cx, float cy, float radius, Color color, int w, int h)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using var brush = new PathGradientBrush(path)
            {
                CenterPoint = new PointF(cx, cy),
                CenterColor = color,
                SurroundColors = new[] { Color.FromArgb(0, color.R, color.G, color.B) },
            };
            var bounds = RectangleF.Intersect(new RectangleF(0, 0, w, h),
                new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2));
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
            }
        }

        private static void Seigaiha(Graphics g, float k, int w, int h)
        {
            // bottom: 78px; height: 42%; opacity .6; stroke #d5c8aa @ .07
            float bottom = h - ShellLayout.ActionBarHeight * k;
            float top = bottom - h * 0.42f;
            var region = new RectangleF(0, top, w, bottom - top);

            var state = g.Save();
            g.SetClip(region);
            using var pen = new Pen(Gfx.Rgba(213, 200, 170, 0.07 * 0.6), Math.Max(1f, k));

            float r = 24f * k;
            float tileH = 48f * k;
            for (float oy = top; oy < bottom + tileH; oy += tileH)
            {
                // arcs whose centre sits on the tile's bottom edge, every 24px
                for (float cx = 0; cx <= w + r; cx += r)
                {
                    g.DrawArc(pen, cx - r, oy + tileH - r, r * 2, r * 2, 180f, 180f);
                }
                // arcs centred mid-tile, every 48px
                for (float cx = 0; cx <= w + r * 2; cx += r * 2)
                {
                    g.DrawArc(pen, cx - r, oy + tileH / 2f - r, r * 2, r * 2, 180f, 180f);
                }
            }
            g.Restore(state);
        }

        private static Bitmap NoiseTile()
        {
            if (_noiseTile != null)
            {
                return _noiseTile;
            }

            const int size = 180;
            var tile = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            var data = tile.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var pixels = new int[size * size];
            var rng = new Random(1337);
            for (int i = 0; i < pixels.Length; i++)
            {
                // feTurbulence fractal noise @ opacity .25, layer opacity .22
                int v = 70 + rng.Next(130);
                int a = 3 + rng.Next(9);
                pixels[i] = (a << 24) | (v << 16) | (v << 8) | v;
            }
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            tile.UnlockBits(data);
            _noiseTile = tile;
            return tile;
        }
    }
}
