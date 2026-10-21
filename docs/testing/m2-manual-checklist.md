# Simple Sigh — M2 in-game checklist (RimWorld 1.7)

Status values: `PASS` / `FAIL` / `NOT RUN (no game)`. Build and unit-test results are recorded in
`docs/testing/m2-results.md`; they are not in-game evidence.

## Wall sign (Task 2)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| W1 | Place next to a wall facing it (N/E/S/W) | Allowed; plaque drawn on the wall cell, upright | NOT RUN (no game) |
| W2 | Place facing open floor / a door / a non-wall building | Rejected ("must be placed on a wall" / "cannot support") | NOT RUN (no game) |
| W3 | Place a second wall sign on the same wall face | Rejected ("something is already placed on this wall") | NOT RUN (no game) |
| W4 | Edit text on each rotation | Text centred on the plaque panel, readable, not rotated | NOT RUN (no game) |
| W5 | Wood / stone / steel | Frame tinted by material, panel light | NOT RUN (no game) |
| W6 | Deconstruct the wall behind the sign | Vanilla attachment behaviour; no error; text mesh released | NOT RUN (no game) |
| W7 | Minify and reinstall on another wall | Text, colour, size preserved | NOT RUN (no game) |
| W8 | Pawns walk through the sign cell | Standable, no path cost | NOT RUN (no game) |
| W9 | Save / load | Same text, same wall, same rotation | NOT RUN (no game) |

## Notice board 2×1 (Task 3)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| N1 | Build in wood / granite / steel, rotations N, E, S, W | Landscape board N/S, portrait board E/W; frame tinted, panel light | NOT RUN (no game) |
| N2 | Multi-line Japanese paragraph with manual line breaks | Breaks preserved; long lines wrap; text shrinks to fit; all inside panel | NOT RUN (no game) |
| N3 | Same text on an E/W board | Text upright, refitted to the portrait panel | NOT RUN (no game) |
| N4 | 256 characters | Accepted; small but inside panel. 257 → rejected with counter red | NOT RUN (no game) |
| N5 | Punctuation `、。」` near a wrap point | Never at the start of a line | NOT RUN (no game) |
| N6 | Two boards with different text | Each keeps its own text (per-instance comp) | NOT RUN (no game) |
| N7 | Save / load, minify / reinstall | Text, line breaks, colour, size preserved | NOT RUN (no game) |
| N8 | Enter key in the editor | Inserts a line break (does not close the dialog) | NOT RUN (no game) |

## Rendering quality and regressions (Task 4)

Offline evidence (not in-game): `docs/images/offline-layout-preview.png`, produced by
`tools/preview/render_preview.py` from the mod's own layout code with IPAGothic metrics, composited on the
mod textures with independently written sprite placement. It shows text inside the panel for the basic sign,
the wall sign on N/E/S/W and the notice board on N/E/S/W, including wrapping and the 50 % size setting.

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| Q1 | Camera zoom close / medium / far | Text scales with the board, stays on the panel; far zoom readable or cleanly tiny | NOT RUN (no game) |
| Q2 | UI scale 100 % / 125 % | No effect on world text | NOT RUN (no game) |
| Q3 | Light material (marble, silver) vs dark (slate, plasteel) | Panel stays light; dark text readable on all | NOT RUN (no game) |
| Q4 | Unlit cave / night / lamp nearby | Text darkens and brightens with the scene (queue 3000 < LightOverlay 3100) | NOT RUN (no game) |
| Q5 | Fog of war over the sign | Text hidden | NOT RUN (no game) |
| Q6 | Indoors / outdoors, roofed | No difference except lighting | NOT RUN (no game) |
| Q7 | Wall sign N/E/S/W | Text on the plaque for each rotation (see preview) | NOT RUN (no game) |
| Q8 | Notice board rotations | Upright text, refitted per orientation (see preview) | NOT RUN (no game) |
| Q9 | Japanese punctuation / long lines | Kinsoku respected, nothing outside panel | NOT RUN (no game) |
| Q10 | Change text / colour / size | Exactly one rebuild each (dev inspect counters) | NOT RUN (no game) |
| Q11 | 100 signs at rest | `builds` constant; no per-frame allocation in the draw path (unit-tested for the pure part) | NOT RUN (no game) |
| Q12 | Pawn in front of a sign | Pawn not hidden by text | NOT RUN (no game) |
