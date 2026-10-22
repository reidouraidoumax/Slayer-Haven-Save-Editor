using System;
using System.Windows.Forms;

namespace ToukidenSaveEditor.Theme
{
    /// <summary>
    /// Converts the design's CSS pixel values to device pixels. Every custom control
    /// multiplies its CSS measurements by <see cref="ScaleOf"/>.
    /// </summary>
    internal static class Ui
    {
        /// <summary>
        /// Extra zoom on top of Windows display scaling. 1.0 reproduces the Figma
        /// design 1:1 (its text is small: 8-12px). Try 1.1 or 1.15 if you want the
        /// whole UI a bit larger - everything, including the default window size,
        /// scales with it.
        /// </summary>
        public static float ExtraScale { get; set; } = 1.0f;

        /// <summary>
        /// Extra size for text only (layout boxes stay as designed). 1.0 = exact Figma
        /// sizes; 1.15 makes all text ~15% larger for readability.
        /// </summary>
        public static float TextScale { get; set; } = 1.15f;

        /// <summary>
        /// When true every text style is drawn one weight heavier than the Figma
        /// design (e.g. body 500 -> 600, titles 600 -> 700) for readability.
        /// </summary>
        public static bool HeavierText { get; set; } = true;

        public static float ScaleOf(Control control)
        {
            int dpi = control.DeviceDpi > 0 ? control.DeviceDpi : 96;
            return dpi / 96f * ExtraScale;
        }
    }
}
