# Simple Sigh — RimWorld 1.7 M1 manual verification

**Status: NOT RUN** — This environment does not include the RimWorld executable or .NET 10 SDK.

- [ ] Mod loads without red errors; no game DLL is bundled.
- [ ] Build 1×1 wooden, stony and metallic signs.
- [ ] Name/edit, cancel edit, choose text color.
- [ ] 日本語「研究室」「発電設備 24h」を表示し、化け・位置ずれがない。
- [ ] Text remains *within* the sign panel at close, normal and far zoom and UI scales 100% / 125%.
- [ ] Save, reload, minify/reinstall and confirm text/color persistence.
- [ ] Benchmark 100 signs: no continuous per-frame texture re-creation.
- [ ] World-space glyph and mesh rendering verified (current renderer is screen-space prototype only).

Do not mark the project as M1-complete until all gameplay checks pass.
