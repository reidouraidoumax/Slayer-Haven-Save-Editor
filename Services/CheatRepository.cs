using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ToukidenSaveEditor.Models;

namespace ToukidenSaveEditor.Services
{
    /// <summary>
    /// Loads cheat definitions from the Action= grammar (see Cheats.txt for the full
    /// field reference). Two sources are supported:
    ///   - LoadFromEmbeddedResource: reads a file baked into the assembly at build
    ///     time via an &lt;EmbeddedResource&gt; item in the .csproj. This is what
    ///     MainForm uses - it means a single-file publish has no loose cheat files
    ///     alongside the exe. To change a cheat, edit the .txt file and rebuild.
    ///   - LoadFromFile: reads a real file from disk at runtime. Kept for local
    ///     testing/tooling; not used by the shipped app anymore.
    /// Both funnel through the same Parse() method, so the grammar only lives in one
    /// place.
    /// </summary>
    public static class CheatRepository
    {
        public static List<CheatDefinition> LoadFromEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return new List<CheatDefinition>();
            }

            using var reader = new StreamReader(stream);
            return Parse(reader.ReadToEnd());
        }

        public static List<CheatDefinition> LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                return new List<CheatDefinition>();
            }

            return Parse(File.ReadAllText(path));
        }

        private static List<CheatDefinition> Parse(string text)
        {
            var list = new List<CheatDefinition>();
            CheatDefinition? current = null;

            using var reader = new StringReader(text);
            string? rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int idx = line.IndexOf('=');
                if (idx < 0)
                {
                    continue;
                }

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    if (current != null)
                    {
                        list.Add(current);
                    }
                    current = new CheatDefinition { Name = value };
                    continue;
                }

                if (current == null)
                {
                    // Malformed file (a key before any Name=) - skip until the next entry starts.
                    continue;
                }

                if (key.Equals("Description", StringComparison.OrdinalIgnoreCase))
                {
                    current.Description = value;
                }
                else if (key.Equals("Tag", StringComparison.OrdinalIgnoreCase))
                {
                    current.Tag = value;
                }
                else if (key.Equals("Action", StringComparison.OrdinalIgnoreCase))
                {
                    var action = ParseAction(value);
                    if (action != null)
                    {
                        current.Actions.Add(action);
                    }
                }
            }

            if (current != null)
            {
                list.Add(current);
            }

            return list;
        }

        private static CheatAction? ParseAction(string text)
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return null;
            }

            if (!Enum.TryParse<CheatActionType>(tokens[0], ignoreCase: true, out var type))
            {
                return null;
            }

            var action = new CheatAction { Type = type };

            for (int i = 1; i < tokens.Length; i++)
            {
                var kv = tokens[i].Split('=', 2);
                if (kv.Length != 2)
                {
                    continue;
                }

                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();

                switch (key)
                {
                    case "offset":
                        action.Offset = (int)ParseNumber(val);
                        break;
                    case "length":
                        action.Length = (int)ParseNumber(val);
                        break;
                    case "value":
                        action.Value = unchecked((uint)ParseNumber(val));
                        break;
                    case "count":
                        action.Count = (int)ParseNumber(val);
                        break;
                    case "stride":
                        action.Stride = (int)ParseNumber(val);
                        break;
                    case "step":
                        action.Step = (int)ParseNumber(val);
                        break;
                    case "width":
                        action.Width = (int)ParseNumber(val);
                        break;
                }
            }

            return action;
        }

        private static long ParseNumber(string s)
        {
            s = s.Trim();
            bool negative = s.StartsWith("-");
            if (negative)
            {
                s = s[1..];
            }

            long result = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(s[2..], 16)
                : long.Parse(s);

            return negative ? -result : result;
        }
    }
}