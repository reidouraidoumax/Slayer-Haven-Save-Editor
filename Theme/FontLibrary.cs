using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace ToukidenSaveEditor.Theme
{
    /// <summary>
    /// The five font faces used by the redesign. CSS weights are baked into separate
    /// font files (each registered as "Regular") because GDI+ only knows Regular/Bold.
    /// </summary>
    internal enum Face
    {
        // Weights below are the Figma design's; Ui.HeavierText draws each one step heavier.
        /// <summary>Noto Serif JP 500 - body text (the page default weight).</summary>
        Serif500,
        /// <summary>Noto Serif JP 600 - eyebrows, tabs, row titles, buttons.</summary>
        Serif600,
        /// <summary>Shippori Mincho 500 - the big headings.</summary>
        Mincho500,
        /// <summary>Shippori Mincho 600 - the "TOUKIDEN 2" wordmark.</summary>
        Mincho600,
        /// <summary>Shippori Mincho 700 - decorative kanji (伐, 殺, 鬼).</summary>
        Mincho700,
    }

    /// <summary>
    /// Loads the embedded .ttf files into a PrivateFontCollection once and hands out
    /// cached Font instances sized in device pixels. Nothing is installed system-wide.
    /// If a font can't be loaded (or the Forms Designer can't see the resources) it
    /// falls back to Yu Mincho / the generic serif so the UI still renders.
    /// </summary>
    internal static class FontLibrary
    {
        private static readonly object Gate = new();
        private static PrivateFontCollection? _collection;
        private static readonly Dictionary<string, FontFamily> Families = new();
        private static readonly Dictionary<(Face, int), Font> Cache = new();

        private static readonly string[] Files =
        {
            "TKNotoSerif-500", "TKNotoSerif-600", "TKNotoSerif-700",
            "TKShippori-500", "TKShippori-600", "TKShippori-700", "TKShippori-800",
        };

        /// <summary>Family name for a face, honouring Ui.HeavierText (one weight up).</summary>
        private static string FamilyFor(Face face)
        {
            bool heavy = Ui.HeavierText;
            return face switch
            {
                Face.Serif500 => heavy ? "TK Noto Serif 600" : "TK Noto Serif 500",
                Face.Serif600 => heavy ? "TK Noto Serif 700" : "TK Noto Serif 600",
                Face.Mincho500 => heavy ? "TK Shippori 600" : "TK Shippori 500",
                Face.Mincho600 => heavy ? "TK Shippori 700" : "TK Shippori 600",
                _ => heavy ? "TK Shippori 800" : "TK Shippori 700",
            };
        }

        /// <summary>
        /// Returns a font of the given face whose em size is <paramref name="pixels"/>
        /// device pixels (pass CSS px multiplied by the UI scale). Text is enlarged by
        /// Ui.TextScale unless <paramref name="decorative"/> is true (glyphs that must
        /// fit a fixed frame, like the 伐 brand mark or the 討鬼 seal).
        /// </summary>
        public static Font Get(Face face, float pixels, bool decorative = false)
        {
            if (!decorative)
            {
                pixels *= Ui.TextScale;
            }

            lock (Gate)
            {
                EnsureLoaded();

                // Quantise to 1/100 px so float noise doesn't explode the cache.
                int key = (int)Math.Round(Math.Max(1f, pixels) * 100f);
                if (Cache.TryGetValue((face, key), out var cached))
                {
                    return cached;
                }

                if (!Families.TryGetValue(FamilyFor(face), out var family))
                {
                    family = Fallback();
                }
                var style = family.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular
                          : family.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold
                          : FontStyle.Regular;

                Font font;
                try
                {
                    font = new Font(family, key / 100f, style, GraphicsUnit.Pixel);
                }
                catch (ArgumentException)
                {
                    font = new Font(FontFamily.GenericSerif, key / 100f, FontStyle.Regular, GraphicsUnit.Pixel);
                }

                Cache[(face, key)] = font;
                return font;
            }
        }

        private static void EnsureLoaded()
        {
            if (_collection != null)
            {
                return;
            }

            _collection = new PrivateFontCollection();
            var assembly = typeof(FontLibrary).Assembly;

            foreach (var file in Files)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream("ToukidenSaveEditor.Fonts." + file + ".ttf");
                    if (stream == null)
                    {
                        continue;
                    }

                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    byte[] data = ms.ToArray();

                    // AddMemoryFont needs the memory to stay valid for the lifetime of
                    // the collection, so this block is intentionally never freed (the
                    // fonts live as long as the app, ~200 KB total).
                    IntPtr block = Marshal.AllocCoTaskMem(data.Length);
                    Marshal.Copy(data, 0, block, data.Length);
                    _collection.AddMemoryFont(block, data.Length);
                }
                catch
                {
                    // Ignore - the fallback below covers a missing face.
                }
            }

            foreach (var family in _collection.Families)
            {
                Families[family.Name] = family;
            }
        }

        private static FontFamily Fallback()
        {
            foreach (var name in new[] { "Yu Mincho", "MS Mincho", "Georgia" })
            {
                try
                {
                    var family = new FontFamily(name);
                    if (family.IsStyleAvailable(FontStyle.Regular))
                    {
                        return family;
                    }
                }
                catch (ArgumentException)
                {
                    // not installed
                }
            }

            return FontFamily.GenericSerif;
        }
    }
}
