# Slayer-Haven-Save-Editor
A Windows Forms (.NET 10) tool for editing Toukiden 2 PS4 save files
(`play00_user.bin`). Every offset and cheat below was found by diffing real
save files against each other and confirmed by in-game testing — see
[`SAVE_FORMAT.md`](SAVE_FORMAT.md) for how that was done and how the save's
checksum works.

This is a fan-made tool built through independent reverse engineering. It is
not affiliated with Koei Tecmo or Omega Force. Use at your own risk, and
**always back up your save before applying any cheat.**
<img width="1279" height="899" alt="image" src="https://github.com/user-attachments/assets/c9b94582-b9d0-496f-9f37-c395ac293ea8" />


## Features

- **Import / Export** — load a `play00_user.bin`, apply cheats, export a new
  file. Export always recalculates and repairs the save's checksum
  automatically, so a manually edited file still loads correctly.
- **Hunter Edicts tab** — general cheats (crafting, money, items). Data-driven
  from `Cheats.txt`, baked into the executable at build time.
- **Mitama Archive tab** — mitama ownership and proficiency cheats, same
  mechanism, data-driven from `MitamaCheats.txt`.

Each cheat appears as its own numbered row with a tag chip and a one-click
**Apply** button, which switches to **Prepared ✓** once applied. The bottom
bar keeps a running count of prepared changes until you export.

## Cheats

### Hunter Edicts tab

| Cheat | What it does |
|---|---|
| **Unlock All Craftings** | Unlocks every weapon, armor, and upgrade recipe at the blacksmith — normally gated behind defeating a large number of enemies. |
| **Max Haku (Cash)** | Sets Haku (money) to 99,999,990. |
| **All Items 999** | Sets the quantity of every discovered item to 999. |

### Mitama Archive tab

Mitamas are fallen-hero spirits that boost equipment when equipped. There are
200 official mitamas (ids `0`–`199`); a further 18 ids (`209`–`226`) exist as
DLC-only extras and are not covered by these cheats.

| Cheat | What it does |
|---|---|
| **Own All Mitamas** | Unlocks all 200 official mitamas. Does not pop the collection trophy, so it's safe to use any time. |
| **Own All Mitamas EXCEPT Fujiwara no Hidesato (Trophy)** | Unlocks every mitama except Hidesato, one of the earliest story-acquirable mitamas. Apply on a new game, then defeat the first Ogre outside of town — obtaining Hidesato normally is what triggers the "collect all mitamas" trophy, so leaving him out lets you earn it legitimately. |
| **Mitama Proficiency: 2 Stars + High Progress** | Sets every skill on every owned mitama to 2 stars with maxed progress toward the 3rd. One more "step outside town" trigger per mitama finishes the job in-game (and pops the trophy along the way, if applicable). |
| **Mitama Proficiency: Instant 3 Filled Stars** | Sets every skill on every mitama straight to 3 filled stars — no further in-game action needed. |

> **Note on star caps:** most mitama skills cap at 3 stars, a few cap at 1 —
> this isn't stored anywhere in the save (it's hardcoded per mitama/skill in
> the game itself), so the proficiency cheats above set a uniform value
> across all 9 skill slots. This overshoots any 1-star-capped skill; in
> testing this hasn't caused visible problems, but it's worth knowing about.
