# In-game checklist: Japanese glyphs (#4) and legibility (#5)

Status of every item: **NOT RUN** — no RimWorld executable was available. Fill in PASS / FAIL with
the game version (`1.7.xxxx`), UI language, screen resolution and a screenshot for each line.
Keep a backup of any save you test with.

Setup: install the ZIP built from this branch (it contains `Fonts/BIZUDPGothic-Bold.ttf` and
`Fonts/BIZUDPGothic-OFL.txt`), enable Simple Sigh after Core, enable Development mode for the
inspect-pane counters, and open `Player.log` / the debug log after each step.

## A. Japanese text is drawn, not "?" (#4)

| # | Step | Expected | Result |
|---|---|---|---|
| A1 | Game language **English**. Build a 1×1 sign, enter `研究室`. | 研究室 drawn on the panel, no `?`, no inspect line about missing characters. | NOT RUN |
| A2 | Same with `食料保管庫`, `立入禁止`, `あいうえお アイウエオ`, `ABC 123 ?!`, `倉庫 North`. | All characters drawn; Latin/kana/kanji share one font. | NOT RUN |
| A3 | Game language **日本語**: repeat A1–A2. | Same as A1–A2. | NOT RUN |
| A4 | After A1 renders, edit another sign to a new character (`避難`). | `避難` appears at once; 研究室 on the first sign stays correct (no wrong or blank glyphs). | NOT RUN |
| A5 | Enter 300+ different kanji across several notice boards (copy a long text). | All drawn; if the atlas is ever full a single warning appears in the log and later characters show `?`. | NOT RUN |
| A6 | Save, return to the main menu, load the save. | Text identical, still drawn in Japanese; log has no Simple Sigh error. | NOT RUN |
| A7 | Switch the game language while a map is loaded, return to the map. | Signs still draw correctly. | NOT RUN |
| A8 | Enter a character outside the font (e.g. emoji `😀`). | One `?` for it, the inspect pane says some characters are not in the sign font; saved text unchanged after reload. | NOT RUN |
| A9 | Temporarily rename `Fonts/BIZUDPGothic-Bold.ttf`, start the game. | One Simple Sigh error in the log; signs fall back to the game font (Japanese may show `?`), no crash or per-frame log spam. Restore the file afterwards. | NOT RUN |
| A10 | Glyph orientation at every rotation of the wall sign and both notice board orientations. | Characters upright and not mirrored. | NOT RUN |

## B. Long labels stay readable (#5)

| # | Step | Expected | Result |
|---|---|---|---|
| B1 | 1×1 sign with `研究` and with `A`. | One large row (same as before). | NOT RUN |
| B2 | 1×1 sign with `立入禁止`, `食料保管庫`, `研究施設入口`. | Two balanced rows (`立入`/`禁止`, `研究施`/`設入口`), clearly larger than a single row. | NOT RUN |
| B3 | Untick "Wrap long text automatically" for B2's sign. | Back to one small row; the setting survives save/load and copy/paste. | NOT RUN |
| B4 | `Food Storage`, `Refrigerator`. | `Food`/`Storage` on two rows; `Refrigerator` never split. | NOT RUN |
| B5 | 64 characters on a 1×1 sign. | All characters drawn inside the panel, nothing cut off; the editor shows the 2×1 notice board hint. | NOT RUN |
| B6 | Size slider at 30 % on B2's sign. | Text smaller, still inside the panel. | NOT RUN |
| B7 | Compare B2 at the starting zoom, half zoom and fully zoomed in; record the zoom at which each label becomes readable. | Matches or beats the offline estimate in `legibility.md`. | NOT RUN |
| B8 | Wall sign on all four wall sides, notice board and electronic sign facing N/E/S/W with `研究施設入口・食料保管庫`. | Text inside each panel, upright. | NOT RUN |
| B9 | Erase a sign's text (OK with an empty field). Dev-mode inspect counter `live`. | `live` drops by one; nothing drawn. | NOT RUN |

## C. Performance

| # | Step | Expected | Result |
|---|---|---|---|
| C1 | 100 signs with different Japanese texts; pan and zoom for a minute. | No continuous stutter; dev counter `builds` does not grow while nothing changes. | NOT RUN |
