# Simple Sigh — M1 in-game checklist (RimWorld 1.7)

Status values: `PASS` / `FAIL` / `NOT RUN`. A build or unit-test pass is **not** an in-game pass.
No RimWorld executable was available while implementing, so every row below is `NOT RUN`
until someone runs it in the real game and records the result.

Setup for a tester: copy the repository folder (with `1.7/Assemblies/SimpleSigh.dll` built,
see README) into `RimWorld/Mods/`, enable **Simple Sigh** after Core, and start a dev-mode colony.

## Build and edit (Task 4)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| B1 | Game starts with Simple Sigh enabled | No red errors mentioning SimpleSigh in the log | NOT RUN |
| B2 | Architect → Furniture → Sign, choose Wood / Granite / Steel | Blueprint uses the chosen material; frame is tinted, panel stays light | NOT RUN |
| B3 | Colonist builds each material | Built sign shows the stuff colour on frame and posts | NOT RUN |
| B4 | Select sign → "Edit text" → type `研究室` → OK | Dialog closes; sign text updated | NOT RUN |
| B5 | Edit → type something → Cancel / X / Esc | Sign keeps previous text | NOT RUN |
| B6 | Type 65 characters | Counter turns red, OK shows "too long" message and keeps dialog open | NOT RUN |
| B7 | Paste text containing a line break into the 1×1 sign | Rejected with "line breaks cannot be used" | NOT RUN |
| B8 | Change colour and size, OK | Values stored (visible once rendering is in) | NOT RUN |
| B9 | Copy text from sign A, paste onto sign B | B gets A's text, colour and size | NOT RUN |
| B10 | Non-player sign (e.g. dev-spawned for another faction) | No edit/copy/paste gizmos | NOT RUN |

## Persistence (Task 4)

| # | Case | Expected | Status |
| --- | --- | --- | --- |
| P1 | Save, reload: `研究室`, `Wood A-12`, `発電設備 24h` | Same text, colour, size after load | NOT RUN |
| P2 | Save, reload: `C:\new` and `   ` (three spaces) | Exactly the same text (codec guards against ParseString/XmlDocument) | NOT RUN |
| P3 | Minify (uninstall) the sign, reinstall elsewhere | Text, colour, size preserved | NOT RUN |
| P4 | Deconstruct the sign | No error; resources returned | NOT RUN |
| P5 | Add mod to an existing save | Save loads; signs can be built | NOT RUN |

## Rendering (Tasks 5–6) — see also Task 5/6 sections below

Filled in by Tasks 5 and 6.
