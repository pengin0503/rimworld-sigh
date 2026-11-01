# Simple Sigh

[日本語](README.md) | **English**

A mod that adds signs with editable text to RimWorld 1.7. Harmony is not required.

> **This is a test version. Game startup, text rendering, saving and loading, and powered behavior have not been verified in-game.** Back up existing saves before use. Safe mid-save removal is not guaranteed.

## Signs and features

- Small and wall signs occupy 1×1 and hold up to 64 characters.
- Noticeboards support multiple lines: 2×1 holds 256 characters and the large 3×1 version holds 384.
- Powered displays are 2×1 or 3×1, require Electricity research and power, go blank during outages, and support blinking or scrolling.
- Floor signs occupy 1×1 or 3×1 and draw text on the floor.

Select a constructed sign and choose **Edit text** to set its text, color and size. Copy and paste, bulk editing, alignment, shadows, clipboard operations and favorite colors are available. Use the sign list in the bottom bar to search, edit and locate signs. Mod settings control display distance and other options.

A Japanese font is bundled, but in-game rendering is unverified. Characters missing from the font, such as emoji, may appear as `?`. Long text can be difficult to read on small signs or large signs facing east or west.

## Installation

Download `SimpleSigh-vX.Y.Z.zip` from [Releases](https://github.com/pengin0503/rimworld-sigh/releases), extract `SimpleSigh` into `RimWorld/Mods`, enable it after Core and restart. GitHub's “Source code” archive is not the mod ZIP.

The older `v0.1.0-test.1` lacks the Japanese rendering and text-size fixes and v0.2 features. Check which version is available. If no release is available, a test ZIP may be obtainable from a successful [PR #3 CI run](https://github.com/pengin0503/rimworld-sigh/pull/3/checks). GitHub sign-in may be required and artifacts expire.

The bundled BIZ UDP Gothic font is distributed under the [SIL Open Font License 1.1](Fonts/BIZUDPGothic-OFL.txt).
