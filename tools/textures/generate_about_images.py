"""Generate About/Preview.png (16:9 Workshop/mod-list preview) and About/ModIcon.png.

Both are composed from the mod's own textures and its text layout + glyph atlas (via
tools/preview/render_preview.py), so they show what the layout code produces. They are offline
renders, not RimWorld screenshots.

Usage: python3 tools/textures/generate_about_images.py
Requires: Pillow, NumPy, .NET SDK 10.
"""

import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "preview"))

import render_preview as rp  # noqa: E402

PREVIEW_SIZE = (1280, 720)
ICON_SIZE = 256
BACKGROUND = (86, 104, 66, 255)

# (case, cells across the square tile); tiles are laid out left to right without overlap.
PX = 112
SHOWCASE = [
    ({"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "研究室"}, 2.0),
    ({"def": "SimpleSigh_NoticeBoardLarge", "rotation": 0, "text": "本日の作業予定：\n畑の収穫と倉庫の整理"}, 3.4),
    ({"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "発電所 稼働中\nPOWER 24h"}, 2.4),
    ({"def": "SimpleSigh_FloorSign", "rotation": 0, "text": "出口", "shadow": True, "floor": (214, 206, 186)}, 1.8),
]


def compose_preview(results, atlas):
    preview = Image.new("RGBA", PREVIEW_SIZE, BACKGROUND)
    tiles = [rp.draw_case(case, result, atlas, PX, cells, None, show_panel=False) for (case, cells), result in zip(SHOWCASE, results)]
    gap = (PREVIEW_SIZE[0] - sum(t.width for t in tiles)) // (len(tiles) + 1)
    assert gap >= 16, f"showcase tiles too wide (gap {gap})"
    x = gap
    for tile in tiles:
        preview.alpha_composite(tile, (x, 150 + (PREVIEW_SIZE[1] - 150 - 40 - tile.height) // 2))
        x += tile.width + gap
    draw = ImageDraw.Draw(preview)
    draw.rectangle([0, 0, PREVIEW_SIZE[0], 150], fill=(30, 34, 28, 235))
    title = ImageFont.truetype(rp.FONT, 76)
    subtitle = ImageFont.truetype(rp.FONT, 34)
    draw.text((40, 18), "Simple Sigh", fill=(240, 236, 220), font=title)
    draw.text((44, 100), "日本語も書けるバニラ風の看板・掲示板・電光掲示板・床看板", fill=(220, 214, 190), font=subtitle)
    small = ImageFont.truetype(rp.FONT, 22)
    draw.text((40, PREVIEW_SIZE[1] - 34), "Offline render of the mod's own layout and textures (not an in-game screenshot)",
              fill=(210, 220, 200), font=small)
    return preview.convert("RGB")


def compose_icon(result, atlas):
    case = {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "看板"}
    tile = rp.draw_case(case, result, atlas, ICON_SIZE / 1.1, 1.1, None, show_panel=False)
    icon = tile.resize((ICON_SIZE, ICON_SIZE), Image.LANCZOS)
    return icon.convert("RGBA")


def main():
    cases = [c for c, _ in SHOWCASE] + [{"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "看板"}]
    results, atlas = rp.run_layout(cases)
    about = os.path.join(ROOT, "About")
    compose_preview(results[:-1], atlas).save(os.path.join(about, "Preview.png"), optimize=True)
    compose_icon(results[-1], atlas).save(os.path.join(about, "ModIcon.png"), optimize=True)
    print("wrote About/Preview.png and About/ModIcon.png")


if __name__ == "__main__":
    main()
