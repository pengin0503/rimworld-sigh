# Simple Sigh v0.2 — performance notes (offline)

These are **offline** measurements of the game-independent code (`Tests/SimpleSigh.Text.Tests/PerformanceTests.cs`).
They are **not** in-game frame times: the Waylith renderer, `Graphics.DrawMesh` and mesh uploads are not measured.
In-game FPS with many signs is `NOT RUN (no game)` (row P4 of `v0.2-manual-checklist.md`).

Machine: cloud container, 4 vCPU Intel Xeon @ 2.80 GHz, .NET 10.0.402. Measured 2026-10-24.

| # | What | Result (Debug / Release build of the tests) | Runs when |
| --- | --- | --- | --- |
| P1 | Text layout for 1000 signs with distinct texts (1×1 single-line, 2×1 and 3×1-portrait multi-line panels, bundled font) | 85 ms / 111 ms in total, about 0.1 ms per sign; 15 365 glyph quads | Only when a sign's text, style, rotation or the glyph atlas changes, never per frame |
| P2 | Rasterizing the glyphs for those texts (about 60 distinct characters) | 5–7 ms | Once per character per session |
| P3 | Steady-state draw path: render key + cache lookup | 0 bytes allocated over 10 000 frames (`SignRenderKeyTests`, `SignRenderSlotTests`) | Every frame per visible sign |
| P4 | Scroll effect: offset + clipping for 100 scrolling signs | about 90 µs per frame, 0 bytes allocated over 600 frames | Every frame per visible scrolling sign |

The two runs differ by about 25 %; treat the numbers as an order of magnitude.

## What the numbers mean

- Static signs cost one `DrawMesh` per frame (two with a shadow), plus the allocation-free cache lookup (P3).
  Layout work (P1) happens only after an edit, so even rebuilding every sign on a large map at once
  (for example after the atlas grows) is a one-off pause of roughly 0.1 s per 1000 signs.
- Scrolling signs also rewrite their mesh every frame. The clipping itself is cheap and allocation-free (P4).
  The 1.7 `UnityEngine.Mesh` stub then copies the vertex and UV arrays on assignment (`set_vertices` clones,
  `set_uv` converts to `Vector4[]`) and rebuilds its engine storage on the next draw. So each visible
  scrolling sign allocates two arrays per frame, sized to its text: about 1 KB for a 30-character message.
  This was read from the stub's IL; it could not be measured, because creating a `Mesh` needs the Vulkan
  backend assemblies.
- Blinking signs skip drawing during the off phase and cost nothing extra.
- Things off screen are not drawn by RimWorld (`DynamicDrawManager`), so off-screen scrolling signs cost nothing.

## Limits in the tests

The assertions are deliberately loose so a slow CI runner passes:

- P1 must finish under 5 s.
- P4 must take under 3 s for 600 frames and allocate 0 bytes.
- P3 must allocate 0 bytes.

They catch regressions of an order of magnitude and any per-frame allocation. They do not detect small slowdowns.
