# Save Format & Checksum

This document covers how `play00_user.bin` (PS4) is structured, how its
integrity check works, and the method used to find every offset in this
project. Nothing here comes from official documentation — it's all derived
empirically from diffing real save files and confirming the results in-game.

## The PS4 save is plaintext, not encrypted

Unlike the PC version of Toukiden 2 (which encrypts its save with a
per-save-slot XOR keystream, reverse-engineered separately), the PS4 save
is ordinary structured binary data. Readable strings (mitama names,
UI labels) appear directly in the file, and every value — money, item
counts, mitama stats — is stored as a plain integer or float at a fixed
offset. The only thing standing between a hex editor and a working save
edit is the checksum below.

## The checksum

```python
CHECKSUM_OFFSET = 0x798CF
DATA_LENGTH = CHECKSUM_OFFSET  # sums bytes 0x0 .. 0x798CE inclusive

checksum = sum(data[:DATA_LENGTH]) & 0xFFFFFFFF
```

A plain 32-bit additive checksum — the sum of every byte from offset `0x0`
up to (but not including) `0x798CF`, stored little-endian as a `uint32` at
`0x798CF`–`0x798D2`. It does **not** cover anything after that point (the
last few hundred bytes of the file sit outside the checksum entirely).

This was confirmed directly: computing the sum against several real save
files and comparing it to the stored value matched exactly every time,
across saves with different Haku amounts, different item states, and
different mitama data.

Any tool that edits bytes before `0x798CF` must recompute and rewrite this
checksum, or the game will refuse to load the save as corrupted. This
editor does it automatically on every export.

## How the offsets were found

Every address in this project — Haku, item quantities, the blacksmith
unlock table, the mitama table — was found the same way:

1. **Make one isolated, known change.** Change exactly one thing in-game
   (set Haku to a specific amount, equip one mitama, level one skill) and
   save.
2. **Diff two saves.** Byte-compare a save from just before the change
   against one from just after. Real content changes show up as a handful
   of differing byte regions.
3. **Filter out session noise.** Saving the game — even with nothing
   changed — still touches a few fields on every write: a monotonic
   save-counter, a per-save random-looking nonce, and (naturally) the
   checksum itself, since it covers everything before it. Diffing two
   *consecutive* saves with no player action in between reveals exactly
   which offsets are this kind of noise, so they can be excluded when
   interpreting a real diff.
4. **Interpret the remaining region(s).** Match the byte width and value
   against what was actually changed in-game (e.g. a `uint32` that reads
   exactly as the new Haku amount) to confirm the offset and encoding.
5. **Cross-validate when possible.** Where a value appears in a repeating
   structure (an array of items, an array of mitamas), diffing a save with
   many entries populated — and checking several *known* entries by name
   and stat — confirms the record size (stride) and field layout in one
   pass, rather than one offset at a time.

## Known structures

### Player status block (around `0x41EDD`–`0x41EEB`)

| Field | Offset | Type |
|---|---|---|
| Equipped mitama — weapon slot | `0x41EDD` | `uint16`, `0xFFFF` = none, else mitama id |
| Equipped mitama — armor slot | `0x41EDF` | same |
| Equipped mitama — demon hand slot | `0x41EE1` | same |
| Haku (money) | `0x41EEB` | `uint32`, little-endian |

### Blacksmith unlock table

A 3000-byte flag table at `0x49A73`. Every byte set to `0x01` unlocks the
corresponding weapon/armor/upgrade recipe for crafting.

### Item quantity table

Spans roughly `0x48303`–`0x4A62B`. Each discovered item occupies a 4-byte
slot: the low 16 bits are a sequential item-index, the high 16 bits are the
quantity. Not every index in that range is a valid item — a handful of
individual slots sit outside the main contiguous run and need separate,
specific writes rather than one flat repeat.

### Mitama table

Base address `0x51A6D`, one 49-byte (`0x31`) record per mitama, indexed
directly by id (`0`–`199` for the 200 official mitamas; ids `200`–`208`
don't exist, `209`–`226` are DLC-only extras not covered by this project).

```
0x00-0x23   9 x float32   skill proficiency (slots 0-8)
0x24-0x2C   9 x byte      skill level / stars (slots 0-8)
0x2D        byte          status: 0x00 undiscovered, 0x0B unlocked
                           (0x06/0x07 = newly seen/unseen, 0x10/0x1B = DLC variants)
0x2E-0x30   3 x byte      equipped boost 1-3 -> skill slot index (0-8), 0xFF = none
```

Each mitama's 9 skill slots map to specific named skills (proficiency,
level, equip-boost reference), but which real skill occupies which slot
number is hardcoded per mitama in the game itself — it isn't stored in the
save. The equipped-boost bytes are what let you work out, per save, which
slot currently holds which of a given mitama's skills.

Two behaviors worth knowing if you're extending this table:

- **Proficiency alone does not instantly max a skill.** The in-game
  "step outside town" recalculation grants exactly one star per trigger
  and resets proficiency to `0` regardless of how much was stored — a
  huge proficiency value doesn't skip levels, it's discarded past the
  first one.
- **Per-skill star caps aren't stored in the save.** Most skills cap at 3
  stars, some cap at 1, and nothing distinguishes them in the data — the
  cap is hardcoded per mitama/skill in the game.

## Repeater writes (Save Wizard Type 4 equivalent)

Several cheats here touch 200 records at once (every mitama's status byte,
or one proficiency slot across all mitamas). Rather than writing each one
individually, these use a single strided write: start address, a fixed
step between writes (the record's stride, `0x31` for mitamas), and a
repeat count. This project's `Sequence` action type (see the main
[README](README.md)) is the in-app equivalent of a Save Wizard Type 4
repeater code.

One quirk worth documenting: a strided write covering a full `0`-`199`
range can silently drop its very last entry under some conditions (seen
specifically with the mitama ownership status byte — id `199`, Admiral
Perry, wasn't set even though the write parameters were correct). The
cause wasn't conclusively identified — it may be specific to how certain
tools execute a repeater's final iteration, or a game-side display/count
quirk rather than the byte itself being wrong. The practical fix used
throughout this project is to follow a full-range repeater with one
explicit, individual write forcing the last entry, rather than relying on
the repeater alone to reach it.
