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
