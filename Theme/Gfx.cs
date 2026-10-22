using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace ToukidenSaveEditor.Theme
{
    /// <summary>
    /// Small GDI+ toolkit used by every custom-painted control: CSS-style colours,
    /// text drawn inside a CSS "line box" with letter-spacing, word wrapping, and the
    /// soft box-shadows the Figma design relies on.
    /// </summary>
    internal static class Gfx
    {
        /// <summary>
        /// Text anti-aliasing used for both drawing and measuring. ClearTypeGridFit gives
        /// the crispest, most readable small text on Windows (every surface the text is
        /// drawn on is opaque, which ClearType needs). Use AntiAlias for a softer,
        /// browser-like look.
        /// </summary>
        public const TextRenderingHint TextHint = TextRenderingHint.ClearTypeGridFit;

        private static readonly StringFormat Typo = CreateTypo();
        private static readonly Bitmap MeasureBitmap = new(1, 1);
        private static readonly Graphics Measurer = CreateMeasurer();
        private static readonly Dictionary<(Font, char), float> CharWidths = new();
        private static readonly Dictionary<(Font, string), float> StringWidths = new();

        // ------------------------------------------------------------------ colours

        public static Color Rgba(int r, int g, int b, double a = 1.0)
            => Color.FromArgb(Math.Clamp((int)Math.Round(a * 255.0), 0, 255), r, g, b);

        public static Color Hex(string hex, double a = 1.0)
        {
            hex = hex.TrimStart('#');
            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return Rgba(r, g, b, a);
        }

        public static Color WithAlpha(Color c, double a) => Rgba(c.R, c.G, c.B, a);

        // ------------------------------------------------------------------ setup

        /// <summary>CSS clamp(min, value, max).</summary>
        public static float Clamp(float min, float value, float max)
            => Math.Max(min, Math.Min(max, value));

        public static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextHint;
            // NOT HighQuality: that is GDI+'s gamma-corrected blending, which computes
            // wrong alpha when drawing onto semi-transparent pixels and caused blocky
            // pink/green/yellow artifacts on the parchment panel.
            g.CompositingQuality = CompositingQuality.AssumeLinear;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        // ------------------------------------------------------------------ text metrics

        /// <summary>The CSS "line-height: normal" of a font, in pixels.</summary>
        public static float NormalLineHeight(Font f)
        {
            var fam = f.FontFamily;
            return fam.GetLineSpacing(f.Style) * f.Size / fam.GetEmHeight(f.Style);
        }

        /// <summary>Height of the font's content area (ascent + descent), in pixels.</summary>
        public static float ContentHeight(Font f)
        {
            var fam = f.FontFamily;
            float em = fam.GetEmHeight(f.Style);
            return (fam.GetCellAscent(f.Style) + fam.GetCellDescent(f.Style)) * f.Size / em;
        }

        public static float Ascent(Font f)
        {
            var fam = f.FontFamily;
            return fam.GetCellAscent(f.Style) * f.Size / fam.GetEmHeight(f.Style);
        }

        /// <summary>
        /// Width of <paramref name="text"/> with CSS letter-spacing
        /// <paramref name="tracking"/> (px added after every character, like browsers do).
        /// </summary>
        public static float Measure(string text, Font font, float tracking = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            if (tracking == 0f)
            {
                if (!StringWidths.TryGetValue((font, text), out float w))
                {
                    w = Measurer.MeasureString(text, font, PointF.Empty, Typo).Width;
                    if (StringWidths.Count > 4000)
                    {
                        StringWidths.Clear();
                    }
                    StringWidths[(font, text)] = w;
                }
                return w;
            }

            float total = 0f;
            foreach (char ch in text)
            {
                total += CharWidth(font, ch) + tracking;
            }
            return total;
        }

        private static float CharWidth(Font font, char ch)
        {
            if (!CharWidths.TryGetValue((font, ch), out float w))
            {
                w = Measurer.MeasureString(ch.ToString(), font, PointF.Empty, Typo).Width;
                CharWidths[(font, ch)] = w;
            }
            return w;
        }

        // ------------------------------------------------------------------ text drawing

        /// <summary>
        /// Draws one line of text vertically centred inside a CSS line box that starts at
        /// <paramref name="lineTop"/> and is <paramref name="lineHeight"/> tall - the same
        /// way a browser positions the glyphs. Returns the advance width.
        /// </summary>
        public static float DrawText(Graphics g, string text, Font font, Color color,
            float x, float lineTop, float lineHeight, float tracking = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            float y = lineTop + (lineHeight - ContentHeight(font)) / 2f;
            using var brush = new SolidBrush(color);

            if (tracking == 0f)
            {
                g.DrawString(text, font, brush, x, y, Typo);
                return Measure(text, font);
            }

            float cx = x;
            foreach (char ch in text)
            {
                if (ch != ' ')
                {
                    g.DrawString(ch.ToString(), font, brush, cx, y, Typo);
                }
                cx += CharWidth(font, ch) + tracking;
            }
            return cx - x;
        }

        /// <summary>Draws text centred horizontally on <paramref name="centerX"/>.</summary>
        public static void DrawTextCentered(Graphics g, string text, Font font, Color color,
            float centerX, float lineTop, float lineHeight, float tracking = 0f)
        {
            float w = Measure(text, font, tracking);
            DrawText(g, text, font, color, centerX - w / 2f, lineTop, lineHeight, tracking);
        }

        /// <summary>Greedy word wrap using the same measurement as DrawText.</summary>
        public static List<string> Wrap(string text, Font font, float tracking, float maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return lines;
            }

            var words = text.Split(' ');
            string current = string.Empty;

            foreach (var word in words)
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && Measure(candidate, font, tracking) > maxWidth)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            return lines;
        }

        /// <summary>CSS text-overflow: ellipsis for a single line.</summary>
        public static string Ellipsize(string text, Font font, float tracking, float maxWidth)
        {
            if (Measure(text, font, tracking) <= maxWidth)
            {
                return text;
            }

            const string ellipsis = "…";
            for (int len = text.Length - 1; len > 0; len--)
            {
                string candidate = text.Substring(0, len).TrimEnd() + ellipsis;
                if (Measure(candidate, font, tracking) <= maxWidth)
                {
                    return candidate;
                }
            }
            return ellipsis;
        }

        // ------------------------------------------------------------------ shapes

        public static void Hairline(Graphics g, Color color, float x1, float y, float x2, float thickness = 1f)
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, x1, y, x2 - x1, thickness);
        }

        /// <summary>1px (or thicker) border drawn on the inside of a rectangle, like CSS border.</summary>
        public static void Border(Graphics g, Color color, RectangleF r, float width = 1f)
        {
            using var pen = new Pen(color, width);
            g.DrawRectangle(pen, r.X + width / 2f, r.Y + width / 2f, r.Width - width, r.Height - width);
        }

        /// <summary>
        /// Approximates CSS box-shadow: 0 offsetY blur color (no spread) by stacking
        /// translucent rectangles from +blur to -blur around the box.
        /// </summary>
        public static void DropShadow(Graphics g, RectangleF box, float offsetY, float blur, Color color)
        {
            if (blur <= 0f || color.A == 0)
            {
                return;
            }

            const int layers = 24;
            double target = color.A / 255.0;
            double perLayer = 1.0 - Math.Pow(1.0 - target, 1.0 / layers);
            using var brush = new SolidBrush(Rgba(color.R, color.G, color.B, Math.Max(perLayer, 1.0 / 255.0)));

            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            for (int i = 0; i < layers; i++)
            {
                float d = blur * (1f - 2f * (i + 0.5f) / layers);
                var r = RectangleF.Inflate(box, d, d);
                r.Offset(0, offsetY);
                if (r.Width > 0 && r.Height > 0)
                {
                    g.FillRectangle(brush, r);
                }
            }
            g.SmoothingMode = oldMode;
        }

        /// <summary>
        /// Approximates CSS box-shadow: inset 0 0 blur color - a darkening that fades
        /// smoothly from the box edges inward (four gradient strips, no banding).
        /// </summary>
        public static void InsetShadow(Graphics g, RectangleF box, float blur, Color color)
        {
            if (blur <= 0f || color.A == 0)
            {
                return;
            }

            blur = Math.Min(blur, Math.Min(box.Width, box.Height) / 2f);
            // A blurred inset shadow is half the colour's strength right at the edge.
            Color edge = Color.FromArgb(Math.Max(1, color.A / 2), color.R, color.G, color.B);
            Color clear = Color.FromArgb(0, color.R, color.G, color.B);

            void Strip(RectangleF r, Color from, Color to, LinearGradientMode mode)
            {
                if (r.Width <= 0 || r.Height <= 0)
                {
                    return;
                }
                using var brush = new LinearGradientBrush(r, from, to, mode) { WrapMode = WrapMode.TileFlipXY };
                g.FillRectangle(brush, r);
            }

            Strip(new RectangleF(box.X, box.Y, box.Width, blur), edge, clear, LinearGradientMode.Vertical);
            Strip(new RectangleF(box.X, box.Bottom - blur, box.Width, blur), clear, edge, LinearGradientMode.Vertical);
            Strip(new RectangleF(box.X, box.Y, blur, box.Height), edge, clear, LinearGradientMode.Horizontal);
            Strip(new RectangleF(box.Right - blur, box.Y, blur, box.Height), clear, edge, LinearGradientMode.Horizontal);
        }

        /// <summary>
        /// The .status-dot element: a 6px dot with a faint 14px ring (35% opacity) in the
        /// "current colour".
        /// </summary>
        public static void StatusDot(Graphics g, float cx, float cy, float k, bool ready)
        {
            Color fill = ready ? Palette.DotReady : Palette.DotIdle;
            Color ring = ready ? Palette.DotReady : Palette.StatusText;

            float r = 3f * k;
            using (var b = new SolidBrush(fill))
            {
                g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
            }

            float rr = 7f * k - 0.5f * k;
            using var pen = new Pen(WithAlpha(ring, 0.35), 1f * k);
            g.DrawEllipse(pen, cx - rr, cy - rr, rr * 2, rr * 2);
        }

        // ------------------------------------------------------------------ internals

        private static StringFormat CreateTypo()
        {
            var sf = (StringFormat)StringFormat.GenericTypographic.Clone();
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
            sf.Trimming = StringTrimming.None;
            return sf;
        }

        private static Graphics CreateMeasurer()
        {
            var g = Graphics.FromImage(MeasureBitmap);
            g.TextRenderingHint = TextHint;
            g.PageUnit = GraphicsUnit.Pixel;
            return g;
        }
    }
}
