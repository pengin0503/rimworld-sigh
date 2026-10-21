# RimWorld 1.7 reference archive — audit (2026-10-21)

## Purpose and provenance

This is a **reference inventory, not an implementation**. It records what was verified from the user-supplied `rimworld_DLL.zip` for the **Simple Sigh** M1–M2 work. The archive is **not committed**. The user reports all files were obtained from their RimWorld 1.7 install; the binary build number was not independently verified by running the game.

- Uploaded archive: `rimworld_DLL.zip`
- ZIP SHA-256: `dab2b7f3d29b87691a6c9e979a3c971814c8c2cfd305714cc4229b1679eba279`
- Archive size: 10,698,917 bytes; expanded file sizes total: 33,173,126 bytes
- ZIP entries: 1,551 including directories; 1,374 regular files
- Every entry is nested under `rimworld_DLL/`
- ZIP CRC check: **passed** (no damaged member reported)
- ZIP path traversal check: **passed** (no absolute paths, backslashes or `..` components detected)
- `Data/Core/Defs`: 554 XML files parsed without an XML syntax error. This is a syntax check, **not** a claim that RimWorld Def resolution or cross-references succeed.

## Reference assemblies present

All five DLLs are directly inside the archive's `rimworld_DLL/` directory, **not** inside `RimWorldWin64_Data/Managed`.

| Relative file inside archive | Size, bytes | SHA-256 |
| --- | ---: | --- |
| `rimworld_DLL/Assembly-CSharp.dll` | 15,632,896 | `69cf35594dcc55d1d77cae482887e880ec2d7c043e139d54d5c59b2dd2b940d4` |
| `rimworld_DLL/Waylith.dll` | 1,544,192 | `30e52fca5f3d62d900fa5a8ac6848bd578dda7b8cdabf4961629b0fa56524b4c` |
| `rimworld_DLL/UnityStubs.dll` | 961,536 | `207a76f55695d859691aef49818156725e2e6e94937cc6867b2e568b4be77ac6` |
| `rimworld_DLL/0Harmony.dll` | 2,532,864 | `c8ef7856b2be3cf24b345c9b5c2f197f68fcf92649635064fce7b3ccc8f77cb0` |
| `rimworld_DLL/netstandard.dll` | 90,624 | `79e9427583a21c268ce0948c726082bd9e2b4703c97a36ed35bb3248f5f7e57e` |

For M1/M2, start by referencing Assembly-CSharp, Waylith, UnityStubs. The remaining two are **conditional build references**, not evidence that Harmony must be a runtime dependency. If Harmony patches are needed, declare `brrainz.harmony` in `About/About.xml`; do not ship `0Harmony.dll` as part of Simple Sigh.

## Game data available for read-only API/Def study

- `rimworld_DLL/Data/Core/Defs/ThingDefs_Buildings/Buildings_Base.xml` — building base definitions
- `.../ThingDefs_Buildings/Buildings_Structure.xml` — wall and column definitions
- `.../ThingDefs_Buildings/Buildings_Furniture.xml` — Stuffable furniture, tables, wall torch examples
- `.../ThingDefs_Buildings/Buildings_Power.xml` — powered building definitions
- `rimworld_DLL/Data/Core/Languages/English/Keyed/` — English in-game UI strings
- `rimworld_DLL/Data/Core/Languages/Japanese (日本語).tar` — Japanese localization archive; contains 623 regular files under `DefInjected/`, `Keyed/`, `Strings/`, etc.
- Core Defs also contain shader-related definition files and shader cache data. These are references, **not** an executable graphics testbed.

The archive contains **no game executable** and **no ordinary building sprite texture set**. Apart from an English language icon there are no PNG assets. There is also no standalone Japanese font supplied as a usable sign texture atlas. A working Japanese sign face therefore still needs actual 1.7 font/renderer validation in a running game.

## Unpacking and SDK setup — private working environment only

1. Supply the same ZIP to the **Opus 5.5 execution environment** separately (or grant access to a private local copy). A chat attachment or a GitHub link to this document is **not** the game binary itself.
2. Extract privately, outside the GitHub repository where practical. After extraction, choose the nested `rimworld_DLL` folder containing `Assembly-CSharp.dll`, `Waylith.dll`, `UnityStubs.dll` as the **reference root**.
3. Let the C# build configuration accept a reference directory (suggested name: `RIMWORLD_REFERENCE_DIR`). The existing M1 plan names `RIMWORLD_GAME_DIR`; the supplied reference directory may satisfy compile-time references, but it is **not a playable installation**, so do not use that variable as proof of a full game install.
4. Install the appropriate .NET SDK separately if needed; the ZIP does not contain the SDK. Verify the actual assembly API signatures rather than assuming 1.6 method names.
5. Do not commit, add to releases, embed in a mod archive or distribute any DLL or translated/base game data taken from this ZIP. `.gitignore` blocks common local extraction paths, but check `git status` and staged files as well.

## Verified vs unverified

**Verified here:** ZIP content, CRC, path safety, assembly filenames/sizes/hashes, Def XML syntax, presence of English and Japanese localization references.

**Not verified here:** actual RimWorld version metadata, C# compilation, Waylith world-space Japanese glyph rendering, game launch, wall placement, load order, save compatibility, lighting, framerate and 1.7 runtime correctness.

**Hand-off gate:** An isolated compile against these references can establish a syntactically/buildable assembly, **not** a working M1/M2 mod. Mark unavailable in-game checks as `NOT RUN`; do not label them `PASS`.
