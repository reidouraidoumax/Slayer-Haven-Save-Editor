using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using ToukidenSaveEditor.Models;
using ToukidenSaveEditor.Services;
using ToukidenSaveEditor.Theme;

namespace ToukidenSaveEditor.UI
{
    /// <summary>One tab of the workbench ("Hunter Edicts", "Mitama Archive").</summary>
    public sealed class WorkbenchTab
    {
        public WorkbenchTab(string title, IEnumerable<CheatDefinition> cheats)
        {
            Title = title;
            Cheats = cheats.ToList();
        }

        public string Title { get; }
        public List<CheatDefinition> Cheats { get; }
    }

    public sealed class CheatEventArgs : EventArgs
    {
        public CheatEventArgs(CheatDefinition cheat) => Cheat = cheat;
        public CheatDefinition Cheat { get; }
    }

    /// <summary>
    /// The parchment panel on the right (.editor): "SLAYER'S WORKBENCH" eyebrow,
    /// "Save alterations" heading, the rotated 討鬼 seal, underline tabs with count
    /// badges, and one numbered row per cheat with its tag chip and Apply button.
    /// Rows are owner-drawn (so they can sit on the paper texture) and scroll with the
    /// mouse wheel if a tab ever has more cheats than fit.
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class Workbench : PaintedControl
    {
        private readonly List<WorkbenchTab> _tabs = new();
        private readonly HashSet<CheatDefinition> _applied = new();
        private int _activeTab;
        private float _scroll;
        private float _contentHeight;

        // hit-test caches (device px, control coordinates), refreshed on every paint
        private readonly List<RectangleF> _tabRects = new();
        private readonly List<RowGeometry> _rows = new();
        private RectangleF _listClip;

        private int _hoverTab = -1;
        private int _hoverRow = -1;
        private bool _hoverButton;

        private Bitmap? _paper;
        private float _paperK;

        private sealed class RowGeometry
        {
            public CheatDefinition Cheat = null!;
            public int Index;
            public float Top;          // relative to the list origin (before scrolling)
            public float Height;
            public float CenterOffset; // centre of the content box, relative to Top
            public float ButtonWidth;
            public float ButtonHeight;
            public List<string> TitleLines = new();
            public bool TagOnOwnLine;
            public float TitleBlockHeight;
            public List<string> DescLines = new();
            public float CopyHeight;
        }

        public Workbench()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                // Show real content in the Forms Designer.
                try
                {
                    SetTabs(new[]
                    {
                        new WorkbenchTab("Hunter Edicts", CheatRepository.LoadFromEmbeddedResource("ToukidenSaveEditor.Cheats.txt")),
                        new WorkbenchTab("Mitama Archive", CheatRepository.LoadFromEmbeddedResource("ToukidenSaveEditor.MitamaCheats.txt")),
                    });
                }
                catch
                {
                    // designer preview only
                }
            }
        }

        /// <summary>Raised when the user clicks a row's Apply button.</summary>
        public event EventHandler<CheatEventArgs>? ApplyRequested;

        [Category("Appearance")] [DefaultValue("SLAYER'S WORKBENCH")]
        public string Eyebrow { get; set; } = "SLAYER'S WORKBENCH";

        [Category("Appearance")] [DefaultValue("Save alterations")]
        public string Heading { get; set; } = "Save alterations";

        [Category("Appearance")] [DefaultValue("Apply the ones you need, then seal your changes by exporting")]
        public string Intro { get; set; } = "Apply the ones you need, then seal your changes by exporting";

        [Category("Appearance")] [DefaultValue("討鬼")]
        public string SealText { get; set; } = "討鬼";

        [Category("Appearance")] [DefaultValue("Apply")]
        public string ApplyText { get; set; } = "Apply";

        [Category("Appearance")] [DefaultValue("Prepared")]
        public string AppliedText { get; set; } = "Prepared";

        [Browsable(false)]
        public int AppliedCount => _applied.Count;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ActiveTabIndex
        {
            get => _activeTab;
            set
            {
                int clamped = _tabs.Count == 0 ? 0 : Math.Clamp(value, 0, _tabs.Count - 1);
                if (clamped == _activeTab)
                {
                    return;
                }
                _activeTab = clamped;
                _scroll = 0;
                _hoverRow = -1;
                _hoverButton = false;
                Invalidate();
            }
        }

        public void SetTabs(IEnumerable<WorkbenchTab> tabs)
        {
            _tabs.Clear();
            _tabs.AddRange(tabs);
            _activeTab = 0;
            _scroll = 0;
            _applied.Clear();
            Invalidate();
        }

        public void MarkApplied(CheatDefinition cheat)
        {
            _applied.Add(cheat);
            Invalidate();
        }

        public void ClearApplied()
        {
            _applied.Clear();
            Invalidate();
        }

        // =====================================================================
        // Painting
        // =====================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            PaintBackdrop(g);

            float k = K;
            EnsurePaper(k);
            if (_paper != null)
            {
                g.DrawImage(_paper, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, _paper.Width, _paper.Height), GraphicsUnit.Pixel);
            }

            Gfx.Prepare(g);

            float vw = ViewportWidth, vh = ViewportHeight;
            float padTop = Gfx.Clamp(25f, 0.03f * vh, 38f);
            float padX = Gfx.Clamp(25f, 0.03f * vw, 45f);
            float left = padX * k;
            float right = Width - padX * k;

            // ---------------- header
            float y = padTop;
            y += Sidebar.DrawEyebrow(g, Eyebrow, Palette.Vermilion, padX, y, k) + 15f;

            float h2Size = Gfx.Clamp(25f, 0.03f * vw, 34f);
            var h2 = FontLibrary.Get(Face.Mincho500, h2Size * k);
            float h2Lh = Gfx.NormalLineHeight(h2);
            Gfx.DrawText(g, Heading, h2, Palette.Ink, left, y * k, h2Lh, 0.09f * h2Size * k);
            y += h2Lh / k;

            SizeF seal = SealSize(k);
            y += 9f;
            var intro = FontLibrary.Get(Face.Serif500, 12f * k);
            float introMax = Math.Max(50f * k, right - left - seal.Width - 16f * k);
            foreach (var line in Gfx.Wrap(Intro, intro, 0, introMax))
            {
                Gfx.DrawText(g, line, intro, Palette.FadedInk, left, y * k, 22.8f * Ui.TextScale * k);
                y += 22.8f * Ui.TextScale;
            }

            DrawSeal(g, right - seal.Width, padTop * k, seal, k);

            // ---------------- tabs
            y += Gfx.Clamp(28f, 0.04f * vh, 40f);
            float tabsTop = y * k;
            float tabsBottom = DrawTabs(g, left, right, tabsTop, k);

            // ---------------- rows
            float listTop = tabsBottom + Math.Max(1f, k);
            float listBottom = Height - 24f * k;
            _listClip = new RectangleF(left, listTop, right - left, Math.Max(0f, listBottom - listTop));
            LayoutRows(left, right, k);

            float maxScroll = Math.Max(0f, _contentHeight - _listClip.Height);
            _scroll = Math.Clamp(_scroll, 0f, maxScroll);

            var state = g.Save();
            g.SetClip(_listClip);
            for (int i = 0; i < _rows.Count; i++)
            {
                DrawRow(g, _rows[i], left, right, listTop - _scroll, k, i == _hoverRow, i == _hoverRow && _hoverButton);
            }
            g.Restore(state);

            if (maxScroll > 0f && _listClip.Height > 0f)
            {
                DrawScrollbar(g, right, maxScroll, k);
            }
        }

        private void EnsurePaper(float k)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            if (_paper != null && _paper.Width == Width && _paper.Height == Height && _paperK == k)
            {
                return;
            }

            _paper?.Dispose();
            _paperK = k;
            // Opaque bitmap: the design's paper is 98% opaque, so the colours below are
            // pre-blended over the sumi background instead of drawing translucently
            // (translucent GDI+ layers were what produced the blocky colour artifacts).
            _paper = new Bitmap(Width, Height, PixelFormat.Format32bppRgb);
            using var g = Graphics.FromImage(_paper);
            Gfx.Prepare(g);
            g.Clear(Palette.Sumi);

            var box = new RectangleF(0, 0, Width, Height);
            var gradRect = RectangleF.Inflate(box, 1, 1);

            // linear-gradient(rgba(233,224,203,.98), rgba(218,205,178,.98)) over sumi
            using (var paper = new LinearGradientBrush(gradRect, OverSumi(233, 224, 203, 0.98), OverSumi(218, 205, 178, 0.98), LinearGradientMode.Vertical))
            {
                g.FillRectangle(paper, box);
            }

            // linear-gradient(90deg, rgba(255,255,255,.2), transparent 8%, transparent 92%, rgba(85,65,34,.08))
            using (var sheen = new LinearGradientBrush(gradRect, Color.White, Color.White, LinearGradientMode.Horizontal))
            {
                sheen.InterpolationColors = new ColorBlend
                {
                    Colors = new[]
                    {
                        Gfx.Rgba(255, 255, 255, 0.2),
                        Gfx.Rgba(255, 255, 255, 0),
                        Gfx.Rgba(85, 65, 34, 0),
                        Gfx.Rgba(85, 65, 34, 0.08),
                    },
                    Positions = new[] { 0f, 0.08f, 0.92f, 1f },
                };
                g.FillRectangle(sheen, box);
            }

            // box-shadow: inset 0 0 70px rgba(101,78,41,.09)
            Gfx.InsetShadow(g, box, 70f * k, Gfx.Rgba(101, 78, 41, 0.09));

            // ::after - 1px frame inset 8px
            Gfx.Border(g, Gfx.Rgba(72, 62, 45, 0.18), RectangleF.Inflate(box, -8f * k, -8f * k), Math.Max(1f, k));
        }

        private static Color OverSumi(int r, int gr, int b, double a)
        {
            var s = Palette.Sumi;
            return Color.FromArgb(
                (int)Math.Round(r * a + s.R * (1 - a)),
                (int)Math.Round(gr * a + s.G * (1 - a)),
                (int)Math.Round(b * a + s.B * (1 - a)));
        }

        // ---------------------------------------------------------------- seal

        private static SizeF SealSize(float k)
        {
            var font = FontLibrary.Get(Face.Serif600, 14f * k, decorative: true);
            float lh = Gfx.NormalLineHeight(font);
            // vertical-rl: column width = line-height, column length = 2 * (1em + .08em)
            float w = 2f * k + 5f * k + lh + 5f * k + 2f * k;
            float h = 2f * k + 8f * k + 2f * (14f + 1.12f) * k + 8f * k + 2f * k;
            return new SizeF(w, h);
        }

        private void DrawSeal(Graphics g, float x, float y, SizeF size, float k)
        {
            if (string.IsNullOrEmpty(SealText))
            {
                return;
            }

            var state = g.Save();
            g.TranslateTransform(x + size.Width / 2f, y + size.Height / 2f);
            g.RotateTransform(4f);

            var rect = new RectangleF(-size.Width / 2f, -size.Height / 2f, size.Width, size.Height);
            // border: 2px solid rgba(166,59,43,.66); box-shadow: inset 0 0 0 2px rgba(166,59,43,.1)
            Gfx.Border(g, Gfx.Rgba(166, 59, 43, 0.66), rect, 2f * k);
            Gfx.Border(g, Gfx.Rgba(166, 59, 43, 0.1), RectangleF.Inflate(rect, -2f * k, -2f * k), 2f * k);

            var font = FontLibrary.Get(Face.Serif600, 14f * k, decorative: true);
            var color = Gfx.Rgba(166, 59, 43, 0.78);
            float charTop = rect.Top + 10f * k;
            foreach (char ch in SealText)
            {
                Gfx.DrawTextCentered(g, ch.ToString(), font, color, 0f, charTop, 14f * k);
                charTop += (14f + 1.12f) * k;
            }

            g.Restore(state);
        }

        // ---------------------------------------------------------------- tabs

        private float DrawTabs(Graphics g, float left, float right, float top, float k)
        {
            _tabRects.Clear();

            var font = FontLibrary.Get(Face.Serif600, 11f * k);
            var badgeFont = FontLibrary.Get(Face.Serif600, 8f * k);
            float tracking = 0.08f * 11f * k;
            float lh = Gfx.NormalLineHeight(font);
            float buttonH = lh + 14f * k;

            float baseline = top + (lh - Gfx.ContentHeight(font)) / 2f + Gfx.Ascent(font);
            float badgeContentH = Gfx.ContentHeight(badgeFont);
            float badgeContentTop = baseline - Gfx.Ascent(badgeFont);

            float x = left;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                bool active = i == _activeTab;
                string count = tab.Cheats.Count.ToString();

                float textW = Gfx.Measure(tab.Title, font, tracking);
                float badgeTextW = Gfx.Measure(count, badgeFont, tracking);
                float badgeW = badgeTextW + 10f * k + 2f * k;
                float width = textW + 9f * k + badgeW;

                Gfx.DrawText(g, tab.Title, font, active ? Palette.Ink : Palette.TabIdle, x, top, lh, tracking);

                var badge = new RectangleF(x + textW + 9f * k, badgeContentTop - 2f * k, badgeW, badgeContentH + 4f * k);
                Gfx.Border(g, Gfx.Rgba(71, 60, 43, 0.3), badge, Math.Max(1f, k));
                Gfx.DrawText(g, count, badgeFont, Palette.TabBadge, badge.X + 6f * k, badgeContentTop, badgeContentH, tracking);

                _tabRects.Add(new RectangleF(x, top, width, buttonH));
                x += width + 35f * k;
            }

            // .tabs border-bottom
            Gfx.Hairline(g, Gfx.Rgba(71, 60, 43, 0.24), left, top + buttonH, right, Math.Max(1f, k));

            // active underline: 3px vermilion, bottom -1px
            if (_activeTab >= 0 && _activeTab < _tabRects.Count)
            {
                var r = _tabRects[_activeTab];
                using var b = new SolidBrush(Palette.Vermilion);
                g.FillRectangle(b, r.X, top + buttonH - 2f * k, r.Width, 3f * k);
            }

            return top + buttonH;
        }

        // ---------------------------------------------------------------- rows

        private void LayoutRows(float left, float right, float k)
        {
            _rows.Clear();
            _contentHeight = 0;
            if (_activeTab < 0 || _activeTab >= _tabs.Count)
            {
                return;
            }

            var titleFont = FontLibrary.Get(Face.Serif600, 12f * k);
            var tagFont = FontLibrary.Get(Face.Serif500, 7f * k);
            var descFont = FontLibrary.Get(Face.Serif500, 11f * k);
            var btnFont = FontLibrary.Get(Face.Serif500, 10f * k);
            float titleLh = Gfx.NormalLineHeight(titleFont);
            float descLh = 11f * 1.65f * Ui.TextScale * k;
            float btnLh = Gfx.NormalLineHeight(btnFont);
            float titleTrack = 0.04f * 12f * k;

            float y = 0;
            var cheats = _tabs[_activeTab].Cheats;
            for (int i = 0; i < cheats.Count; i++)
            {
                var cheat = cheats[i];
                var row = new RowGeometry { Cheat = cheat, Index = i, Top = y };

                bool applied = _applied.Contains(cheat);
                string label = applied ? AppliedText : ApplyText;
                string glyph = applied ? "✓" : "→";
                row.ButtonWidth = Math.Max(111f * k,
                    2f * k + 24f * k + Gfx.Measure(label, btnFont) + 20f * k + Gfx.Measure(glyph, btnFont));
                row.ButtonHeight = btnLh + 20f * k + 2f * k;

                float copyW = Math.Max(40f * k, (right - left) - 8f * k - 34f * k - 19f * k - 19f * k - row.ButtonWidth);

                // title line (+ tag chip; flex-wrap pushes the tag down if it doesn't fit)
                float tagH = Gfx.ContentHeight(tagFont) + 6f * k;
                float tagW = string.IsNullOrEmpty(cheat.Tag) ? 0f : TagWidth(cheat.Tag, tagFont, k);
                float titleW = Gfx.Measure(cheat.Name, titleFont, titleTrack);
                if (tagW == 0f || titleW + 10f * k + tagW <= copyW)
                {
                    row.TitleLines = Gfx.Wrap(cheat.Name, titleFont, titleTrack, copyW);
                    row.TagOnOwnLine = false;
                    row.TitleBlockHeight = Math.Max(row.TitleLines.Count * titleLh, tagW > 0 ? tagH : 0f);
                }
                else
                {
                    row.TitleLines = Gfx.Wrap(cheat.Name, titleFont, titleTrack, copyW);
                    row.TagOnOwnLine = true;
                    row.TitleBlockHeight = row.TitleLines.Count * titleLh + 10f * k + tagH;
                }

                row.DescLines = string.IsNullOrEmpty(cheat.Description)
                    ? new List<string>()
                    : Gfx.Wrap(cheat.Description, descFont, 0, copyW);
                row.CopyHeight = row.TitleBlockHeight + (row.DescLines.Count > 0 ? 7f * k + row.DescLines.Count * descLh : 0f);

                float inner = Math.Max(row.CopyHeight, Math.Max(row.ButtonHeight, 27f * k));
                row.Height = Math.Max(101f * k, inner + 32f * k + 1f * k);
                row.CenterOffset = 16f * k + (row.Height - 1f * k - 32f * k) / 2f;

                _rows.Add(row);
                y += row.Height;
            }

            _contentHeight = y;
        }

        private static float TagWidth(string tag, Font font, float k)
            => Gfx.Measure(tag.ToUpperInvariant(), font, 0.14f * 7f * k) + 12f * k + 2f * k;

        private void DrawRow(Graphics g, RowGeometry row, float left, float right, float originY, float k, bool hover, bool hoverButton)
        {
            float top = originY + row.Top;
            if (top > _listClip.Bottom || top + row.Height < _listClip.Top)
            {
                return;
            }

            var rowRect = new RectangleF(left, top, right - left, row.Height);
            if (hover)
            {
                using var hb = new SolidBrush(Gfx.Rgba(255, 255, 255, 0.22));
                g.FillRectangle(hb, rowRect);
            }

            // border-bottom: 1px solid rgba(74,62,43,.18)
            Gfx.Hairline(g, Gfx.Rgba(74, 62, 43, 0.18), left, rowRect.Bottom - Math.Max(1f, k), right, Math.Max(1f, k));

            float cy = top + row.CenterOffset;

            // ---- .edit-index: 27px box rotated -2deg
            {
                float cx = left + 4f * k + 13.5f * k;
                var state = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(-2f);
                var box = new RectangleF(-13.5f * k, -13.5f * k, 27f * k, 27f * k);
                Gfx.Border(g, Gfx.Rgba(166, 59, 43, 0.35), box, Math.Max(1f, k));
                var idxFont = FontLibrary.Get(Face.Serif500, 8f * k);
                float lh = Gfx.NormalLineHeight(idxFont);
                Gfx.DrawTextCentered(g, (row.Index + 1).ToString("00"), idxFont, Palette.Vermilion, 0f, -lh / 2f, lh);
                g.Restore(state);
            }

            // ---- .edit-copy
            float copyX = left + 4f * k + 34f * k + 19f * k;
            float copyTop = cy - row.CopyHeight / 2f;
            var titleFont = FontLibrary.Get(Face.Serif600, 12f * k);
            var tagFont = FontLibrary.Get(Face.Serif500, 7f * k);
            var descFont = FontLibrary.Get(Face.Serif500, 11f * k);
            float titleLh = Gfx.NormalLineHeight(titleFont);
            float titleTrack = 0.04f * 12f * k;
            float tagH = Gfx.ContentHeight(tagFont) + 6f * k;

            float titleBlockTop = copyTop;
            float titleTextH = row.TitleLines.Count * titleLh;
            float textTop = row.TagOnOwnLine ? titleBlockTop : titleBlockTop + (row.TitleBlockHeight - titleTextH) / 2f;
            float lastLineRight = copyX;
            foreach (var line in row.TitleLines)
            {
                float w = Gfx.DrawText(g, line, titleFont, Palette.Ink, copyX, textTop, titleLh, titleTrack);
                lastLineRight = copyX + w;
                textTop += titleLh;
            }

            if (!string.IsNullOrEmpty(row.Cheat.Tag))
            {
                float tagX, tagY;
                if (row.TagOnOwnLine)
                {
                    tagX = copyX;
                    tagY = titleBlockTop + titleTextH + 10f * k;
                }
                else
                {
                    tagX = lastLineRight + 10f * k;
                    tagY = titleBlockTop + (row.TitleBlockHeight - tagH) / 2f;
                }
                DrawTag(g, row.Cheat.Tag, tagFont, tagX, tagY, tagH, k);
            }

            float descTop = copyTop + row.TitleBlockHeight + 7f * k;
            float descLh = 11f * 1.65f * Ui.TextScale * k;
            foreach (var line in row.DescLines)
            {
                Gfx.DrawText(g, line, descFont, Palette.FadedInk, copyX, descTop, descLh);
                descTop += descLh;
            }

            // ---- .apply-button
            var btn = ButtonRect(row, right, top, k);
            DrawApplyButton(g, btn, _applied.Contains(row.Cheat), hoverButton, k);
        }

        private static RectangleF ButtonRect(RowGeometry row, float right, float rowTop, float k)
        {
            float cy = rowTop + row.CenterOffset;
            return new RectangleF(right - 4f * k - row.ButtonWidth, cy - row.ButtonHeight / 2f, row.ButtonWidth, row.ButtonHeight);
        }

        private static void DrawTag(Graphics g, string tag, Font font, float x, float y, float h, float k)
        {
            string text = tag.ToUpperInvariant();
            float tracking = 0.14f * 7f * k;
            float w = Gfx.Measure(text, font, tracking) + 12f * k + 2f * k;
            var rect = new RectangleF(x, y, w, h);
            Gfx.Border(g, Gfx.Rgba(68, 58, 42, 0.25), rect, Math.Max(1f, k));
            Gfx.DrawText(g, text, font, Palette.TagText, x + 7f * k, y + 3f * k, h - 6f * k, tracking);
        }

        private void DrawApplyButton(Graphics g, RectangleF r, bool applied, bool hover, float k)
        {
            Color back, border, text, inner;
            if (applied)
            {
                back = Gfx.Rgba(166, 59, 43, 0.08);
                border = Gfx.Rgba(166, 59, 43, 0.55);
                text = Palette.Vermilion;
                inner = hover ? Gfx.Rgba(238, 229, 209, 0.23) : Gfx.Rgba(54, 51, 41, 0.2);
            }
            else if (hover)
            {
                back = Palette.Indigo;
                border = Palette.Indigo;
                text = Palette.BrandText;
                inner = Gfx.Rgba(238, 229, 209, 0.23);
            }
            else
            {
                back = Color.Transparent;
                border = Gfx.Rgba(54, 51, 41, 0.48);
                text = Palette.ApplyText;
                inner = Gfx.Rgba(54, 51, 41, 0.2);
            }

            if (back.A > 0)
            {
                using var b = new SolidBrush(back);
                g.FillRectangle(b, r);
            }
            Gfx.Border(g, border, r, Math.Max(1f, k));
            Gfx.Border(g, inner, RectangleF.Inflate(r, -3f * k, -3f * k), Math.Max(1f, k));

            var font = FontLibrary.Get(Face.Serif500, 10f * k);
            float lh = Gfx.NormalLineHeight(font);
            float ty = r.Y + (r.Height - lh) / 2f;
            Gfx.DrawText(g, applied ? AppliedText : ApplyText, font, text, r.X + 13f * k, ty, lh);
            string glyph = applied ? "✓" : "→";
            float gw = Gfx.Measure(glyph, font);
            Gfx.DrawText(g, glyph, font, text, r.Right - 13f * k - gw, ty, lh);
        }

        private void DrawScrollbar(Graphics g, float right, float maxScroll, float k)
        {
            float trackX = right + 14f * k;
            float trackH = _listClip.Height;
            float thumbH = Math.Max(24f * k, trackH * trackH / (_contentHeight > 0 ? _contentHeight : 1f));
            float thumbY = _listClip.Top + (trackH - thumbH) * (_scroll / maxScroll);
            using var track = new SolidBrush(Gfx.Rgba(71, 60, 43, 0.08));
            using var thumb = new SolidBrush(Gfx.Rgba(71, 60, 43, 0.3));
            g.FillRectangle(track, trackX, _listClip.Top, 3f * k, trackH);
            g.FillRectangle(thumb, trackX, thumbY, 3f * k, thumbH);
        }

        // =====================================================================
        // Input
        // =====================================================================

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateHover(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverRow != -1 || _hoverTab != -1 || _hoverButton)
            {
                _hoverRow = -1;
                _hoverTab = -1;
                _hoverButton = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float maxScroll = Math.Max(0f, _contentHeight - _listClip.Height);
            if (maxScroll <= 0f)
            {
                return;
            }
            _scroll = Math.Clamp(_scroll - e.Delta / 120f * 48f * K, 0f, maxScroll);
            Invalidate();
            UpdateHover(e.Location);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    ActiveTabIndex = i;
                    UpdateHover(e.Location);
                    return;
                }
            }

            var hit = HitRow(e.Location, out bool onButton);
            if (hit != null && onButton)
            {
                ApplyRequested?.Invoke(this, new CheatEventArgs(hit.Cheat));
            }
        }

        private void UpdateHover(Point p)
        {
            int tab = -1;
            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(p))
                {
                    tab = i;
                    break;
                }
            }

            var row = HitRow(p, out bool onButton);
            int rowIndex = row != null ? _rows.IndexOf(row) : -1;

            if (tab != _hoverTab || rowIndex != _hoverRow || onButton != _hoverButton)
            {
                _hoverTab = tab;
                _hoverRow = rowIndex;
                _hoverButton = onButton;
                Invalidate();
            }

            Cursor = (tab >= 0 || onButton) ? Cursors.Hand : Cursors.Default;
        }

        private RowGeometry? HitRow(Point p, out bool onButton)
        {
            onButton = false;
            if (!_listClip.Contains(p))
            {
                return null;
            }

            float k = K;
            float originY = _listClip.Top - _scroll;
            float left = _listClip.Left;
            float right = _listClip.Right;
            foreach (var row in _rows)
            {
                float top = originY + row.Top;
                if (p.Y >= top && p.Y < top + row.Height && p.X >= left && p.X <= right)
                {
                    onButton = ButtonRect(row, right, top, k).Contains(p);
                    return row;
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _paper?.Dispose();
                _paper = null;
            }
            base.Dispose(disposing);
        }
    }
}
