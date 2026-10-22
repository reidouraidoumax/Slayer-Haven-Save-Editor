using System.Collections.Generic;

namespace ToukidenSaveEditor.Models
{
    /// <summary>
    /// How one action writes to the save buffer.
    /// </summary>
    public enum CheatActionType
    {
        /// <summary>Writes the low byte of Value, repeated across Length bytes starting at Offset.</summary>
        Fill,
        /// <summary>Writes a single byte (Value) at Offset.</summary>
        Write8,
        /// <summary>Writes a little-endian 16-bit Value at Offset.</summary>
        Write16,
        /// <summary>Writes a little-endian 32-bit Value at Offset.</summary>
        Write32,
        /// <summary>
        /// Writes Count values of Width bits, starting at Value and incrementing by Step
        /// each time, at addresses Offset, Offset+Stride, Offset+2*Stride, ...
        /// Equivalent to a Save Wizard Type 4 (Multi-Write/Repeater) code.
        /// </summary>
        Sequence
    }

    /// <summary>
    /// One write operation. A cheat is a list of these applied in order, so a single
    /// button can reproduce a multi-line quick code (e.g. a repeater plus a few
    /// standalone writes filling in the gaps).
    /// </summary>
    public class CheatAction
    {
        public CheatActionType Type { get; set; }
        public int Offset { get; set; }

        /// <summary>Byte count for Fill.</summary>
        public int Length { get; set; }

        /// <summary>Value written for Write8/16/32, or the initial value for Sequence.</summary>
        public uint Value { get; set; }

        /// <summary>Total number of writes for Sequence (matches a Save Wizard repeater's loop count + 1).</summary>
        public int Count { get; set; }

        /// <summary>Address step in bytes between successive Sequence writes.</summary>
        public int Stride { get; set; } = 4;

        /// <summary>Value added after each Sequence write (may be negative).</summary>
        public int Step { get; set; }

        /// <summary>Bits per Sequence write: 8, 16, or 32.</summary>
        public int Width { get; set; } = 32;
    }

    /// <summary>
    /// A named cheat, parsed from Cheats.txt. Add more blocks to that file to add
    /// more entries here - the Cheats tab is built from this list at runtime, one
    /// Apply button per cheat.
    /// </summary>
    public class CheatDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional short label shown as a small chip next to the name in the
        /// workbench list (e.g. "Crafting", "Recommended"). Set with a Tag= line.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        public List<CheatAction> Actions { get; set; } = new();
    }
}
