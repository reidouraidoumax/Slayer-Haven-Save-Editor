using System;
using System.IO;
using ToukidenSaveEditor.Models;

namespace ToukidenSaveEditor.Services
{
    /// <summary>
    /// Handles loading a Toukiden 2 save file into memory, applying cheat writes to it,
    /// and exporting it back to disk with a repaired checksum.
    ///
    /// Checksum algorithm confirmed by diffing three real saves (base / cash / blacksmith
    /// unlock) against each other: it is a plain 32-bit additive checksum - the sum of
    /// every byte from offset 0x0 up to (but not including) 0x798CF, stored little-endian
    /// as a uint32 at 0x798CF-0x798D2. It does NOT cover anything after 0x798D2.
    /// </summary>
    public class SaveFileService
    {
        public const int ChecksumOffset = 0x798CF;
        private const int ChecksumLength = 4;

        public byte[]? Buffer { get; private set; }
        public string? CurrentPath { get; private set; }

        public bool IsLoaded => Buffer != null;

        public void Load(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < ChecksumOffset + ChecksumLength)
            {
                throw new InvalidOperationException(
                    $"This doesn't look like a Toukiden 2 save file (only {bytes.Length} bytes - " +
                    $"expected at least {ChecksumOffset + ChecksumLength}).");
            }

            Buffer = bytes;
            CurrentPath = path;
        }

        public void ApplyCheat(CheatDefinition cheat)
        {
            if (Buffer == null)
            {
                throw new InvalidOperationException("No save file loaded.");
            }

            foreach (var action in cheat.Actions)
            {
                ApplyAction(action, cheat.Name);
            }
        }

        private void ApplyAction(CheatAction action, string cheatName)
        {
            switch (action.Type)
            {
                case CheatActionType.Fill:
                {
                    EnsureBounds(action.Offset, action.Length, cheatName);
                    byte fillValue = (byte)(action.Value & 0xFF);
                    for (int i = 0; i < action.Length; i++)
                    {
                        Buffer![action.Offset + i] = fillValue;
                    }
                    break;
                }

                case CheatActionType.Write8:
                    EnsureBounds(action.Offset, 1, cheatName);
                    Buffer![action.Offset] = (byte)(action.Value & 0xFF);
                    break;

                case CheatActionType.Write16:
                    EnsureBounds(action.Offset, 2, cheatName);
                    BitConverter.GetBytes((ushort)(action.Value & 0xFFFF)).CopyTo(Buffer!, action.Offset);
                    break;

                case CheatActionType.Write32:
                    EnsureBounds(action.Offset, 4, cheatName);
                    BitConverter.GetBytes(action.Value).CopyTo(Buffer!, action.Offset);
                    break;

                case CheatActionType.Sequence:
                {
                    int widthBytes = action.Width / 8;
                    if (widthBytes is not (1 or 2 or 4))
                    {
                        throw new InvalidOperationException(
                            $"Cheat '{cheatName}' has a Sequence action with an unsupported Width ({action.Width}).");
                    }
                    if (action.Count <= 0)
                    {
                        break;
                    }

                    int span = action.Stride * (action.Count - 1) + widthBytes;
                    EnsureBounds(action.Offset, span, cheatName);

                    uint value = action.Value;
                    int addr = action.Offset;
                    for (int i = 0; i < action.Count; i++)
                    {
                        WriteWidth(addr, value, widthBytes);
                        addr += action.Stride;
                        value = unchecked((uint)(value + action.Step));
                    }
                    break;
                }
            }
        }

        private void WriteWidth(int offset, uint value, int widthBytes)
        {
            switch (widthBytes)
            {
                case 1:
                    Buffer![offset] = (byte)(value & 0xFF);
                    break;
                case 2:
                    BitConverter.GetBytes((ushort)(value & 0xFFFF)).CopyTo(Buffer!, offset);
                    break;
                case 4:
                    BitConverter.GetBytes(value).CopyTo(Buffer!, offset);
                    break;
            }
        }

        private void EnsureBounds(int offset, int length, string cheatName)
        {
            if (Buffer == null)
            {
                throw new InvalidOperationException("No save file loaded.");
            }
            if (offset < 0 || length < 0 || offset + length > Buffer.Length)
            {
                throw new InvalidOperationException(
                    $"Cheat '{cheatName}' targets a region outside this save file's bounds.");
            }
        }

        /// <summary>
        /// Recomputes the checksum from the current buffer contents without writing it.
        /// </summary>
        public uint RecalculateChecksum()
        {
            if (Buffer == null)
            {
                throw new InvalidOperationException("No save file loaded.");
            }

            uint sum = 0;
            for (int i = 0; i < ChecksumOffset; i++)
            {
                sum += Buffer[i];
            }
            return sum;
        }

        public void FixChecksum()
        {
            if (Buffer == null)
            {
                throw new InvalidOperationException("No save file loaded.");
            }

            uint checksum = RecalculateChecksum();
            BitConverter.GetBytes(checksum).CopyTo(Buffer, ChecksumOffset);
        }

        public void Export(string path)
        {
            if (Buffer == null)
            {
                throw new InvalidOperationException("No save file loaded.");
            }

            FixChecksum();
            File.WriteAllBytes(path, Buffer);
        }
    }
}
