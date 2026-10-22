using System.Drawing;

namespace ToukidenSaveEditor.Theme
{
    /// <summary>
    /// Colours of the "Slayer's Haven" redesign: sumi-ink charcoal, aged washi paper,
    /// vermilion seal-red, indigo and old gold. Values are taken 1:1 from the Figma
    /// Make stylesheet (the CSS custom properties on .app-shell plus the one-off
    /// rgba() values used by individual elements).
    /// </summary>
    internal static class Palette
    {
        // --- CSS custom properties ---------------------------------------------------
        public static readonly Color Sumi = Gfx.Hex("#191b18");
        public static readonly Color SumiSoft = Gfx.Hex("#292b25");
        public static readonly Color Paper = Gfx.Hex("#e6dcc4");
        public static readonly Color PaperLight = Gfx.Hex("#f0e7d2");
        public static readonly Color PaperDark = Gfx.Hex("#cbbd9f");
        public static readonly Color Ink = Gfx.Hex("#312c24");
        public static readonly Color FadedInk = Gfx.Hex("#766d5d");
        public static readonly Color Vermilion = Gfx.Hex("#a63b2b");
        public static readonly Color VermilionBright = Gfx.Hex("#bd4733");
        public static readonly Color Indigo = Gfx.Hex("#283a3d");
        public static readonly Color Gold = Gfx.Hex("#9a8049");

        // --- Dark chrome (top bar, sidebar, action bar) ------------------------------
        /// <summary>1px hairlines on the dark background: rgba(230,220,196,.16).</summary>
        public static readonly Color Hairline = Gfx.Rgba(230, 220, 196, 0.16);
        public static readonly Color BrandText = Gfx.Hex("#eee5d1");
        public static readonly Color BrandSub = Gfx.Hex("#948b79");
        public static readonly Color BrandGlyph = Gfx.Hex("#bca36c");
        public static readonly Color StatusText = Gfx.Hex("#a79d88");
        public static readonly Color DotIdle = Gfx.Hex("#645f54");
        public static readonly Color DotReady = Gfx.Hex("#9b9c69");
        public static readonly Color EyebrowDark = Gfx.Hex("#bd8a70");
        public static readonly Color HeadingLight = Gfx.Hex("#ece2cd");
        public static readonly Color BodyMuted = Gfx.Hex("#9e9583");
        public static readonly Color WardTitle = Gfx.Hex("#d5cab3");
        public static readonly Color WardBody = Gfx.Hex("#8c8474");
        public static readonly Color WardGlyph = Gfx.Hex("#c07460");
        public static readonly Color FooterText = Gfx.Hex("#6f695e");
        public static readonly Color ChangeCount = Gfx.Hex("#b69d6b");
        public static readonly Color ActionBarBack = Gfx.Rgba(15, 16, 14, 0.97);

        // --- Paper surfaces --------------------------------------------------------
        public static readonly Color CardLabel = Gfx.Hex("#7a6d59");
        public static readonly Color CardGlyph = Gfx.Hex("#6b5d43");
        public static readonly Color TabIdle = Gfx.Hex("#8b806c");
        public static readonly Color TabBadge = Gfx.Hex("#877a64");
        public static readonly Color TagText = Gfx.Hex("#847762");
        public static readonly Color ApplyText = Gfx.Hex("#3d3a30");

        // --- Buttons ---------------------------------------------------------------
        public static readonly Color ImportText = Gfx.Hex("#fff2dc");
        /// <summary>Export button colour - the Make file overrides the CSS with an inline teal.</summary>
        public static readonly Color ExportBack = Gfx.Hex("#25cbbb");
        public static readonly Color ExportText = Gfx.Hex("#ffffff");
    }
}
