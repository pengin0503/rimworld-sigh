"""Offline preview for Simple Sigh (NOT an in-game screenshot).

1. Runs tools/preview: the mod's own SignPanelGeometry + SignTextLayout (linked sources) with glyphs
   rasterized from the bundled sign font by the mod's GlyphAtlas, for every sign def and rotation.
2. Composites the result onto the mod's textures. Text is drawn by sampling the mod's glyph atlas
   through each quad's UVs with bilinear filtering and no mipmaps, like the World/WorldText material,
   assuming Unity's texture convention (row 0 and v = 0 at the bottom). A wrong UV or row order shows
   up as upside-down or wrong characters.
   Sprite placement here is written independently from the C# code, following RimWorld's rules
   (TrueCenter, Graphic.DrawOffset, Graphic_Multi east/west draw-size swap and west mirroring), so a
   geometry bug shows up as text off the panel.

Writes two images:
  OUTPUT.png          every def and rotation at 128 px per cell
  OUTPUT-zoom.png     1x1 basic sign labels, one row vs automatic wrapping, at the pixel sizes of a
                      map cell on a 1080p screen at the starting zoom and fully zoomed in, and on 4K
                      fully zoomed in (shown 2x enlarged with nearest-neighbour pixels)

Usage: python3 tools/preview/render_preview.py OUTPUT.png
Requires: Pillow, NumPy, .NET SDK 10.
"""

import json
import os
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEX = os.path.join(ROOT, "Textures", "Things", "Building")
FONT = os.path.join(ROOT, "Fonts", "BIZUDPGothic-Bold.ttf")
PX = 128            # pixels per cell for the def/rotation sheet
TILE_CELLS = 4      # each case is drawn on a 4x4 cell tile
WOOD = (133, 97, 67)
STEEL = (110, 115, 125)
TEXT = (34, 28, 23)

NOTICE = "本日の作業予定：\n午前は畑の収穫、午後は倉庫の整理。夜間は発電設備の点検を行います。"
CASES = [
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "研究室"},
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "研究施設入口"},
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "研究施設入口", "autoWrap": False},
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "Food Storage"},
    {"def": "SimpleSigh_WallSign", "rotation": 0, "text": "食料保管庫"},
    {"def": "SimpleSigh_WallSign", "rotation": 1, "text": "倉庫 East"},
    {"def": "SimpleSigh_WallSign", "rotation": 2, "text": "立入禁止"},
    {"def": "SimpleSigh_WallSign", "rotation": 3, "text": "倉庫 West"},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 0, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 1, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 2, "text": "倉庫\n備品"},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 3, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 0, "text": NOTICE, "scale": 0.5},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "発電所 稼働中\nPOWER 24h"},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 1, "text": "発電所 稼働中\nPOWER 24h"},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "停電中は消灯（文字は保持）", "color": (90, 230, 120)},
]
ZOOM_LABELS = ["研究室", "立入禁止", "食料保管庫", "研究施設入口", "第二研究施設の入口", "Food Storage"]
# Map cell size in screen pixels: 1080 / (2 * camera root size). Root size 24 is CameraDriver.StartingSize
# in the 1.7 reference assemblies; 11 is the vanilla zoom-in limit of earlier versions (unverified for 1.7).
ZOOMS = [("1080p start", 1080 / 48), ("1080p max zoom", 1080 / 22), ("4K max zoom", 2160 / 22)]
GRAPHICS = {
    "SimpleSigh_BasicSign": {"tex": "SimpleSign", "multi": False, "size": (1, 1), "footprint": (1, 1), "offsets": None},
    "SimpleSigh_WallSign": {"tex": "SimpleWallSign", "multi": False, "size": (1, 1), "footprint": (1, 1),
                            "offsets": [(0, 0.9), (0.9, 0), (0, -0.9), (-0.9, 0)]},
    "SimpleSigh_NoticeBoard": {"tex": "SimpleNoticeBoard", "multi": True, "size": (2, 1), "footprint": (2, 1), "offsets": None},
    "SimpleSigh_ElectronicSign": {"tex": "SimpleElectronicSign", "multi": True, "size": (2, 1), "footprint": (2, 1), "offsets": None,
                                  "metal": True, "text": (255, 171, 61)},
}


def tinted(name, mask_name, color):
    tex = Image.open(os.path.join(TEX, name + ".png")).convert("RGBA")
    mask = Image.open(os.path.join(TEX, mask_name + ".png")).convert("RGBA")
    out = Image.new("RGBA", tex.size)
    tp, mp, op = tex.load(), mask.load(), out.load()
    for y in range(tex.size[1]):
        for x in range(tex.size[0]):
            r, g, b, a = tp[x, y]
            p = mp[x, y][0] / 255
            op[x, y] = tuple(int(v * (p * cv / 255 + 1 - p)) for v, cv in zip((r, g, b), color)) + (a,)
    return out


def sample_bilinear(atlas_top_down, u, v):
    """Texture lookup with clamp-to-edge bilinear filtering; v = 0 is the bottom of the texture."""
    h, w = atlas_top_down.shape
    x = u * w - 0.5
    y = (1.0 - v) * h - 0.5
    x0 = np.clip(np.floor(x).astype(int), 0, w - 1)
    y0 = np.clip(np.floor(y).astype(int), 0, h - 1)
    x1 = np.clip(x0 + 1, 0, w - 1)
    y1 = np.clip(y0 + 1, 0, h - 1)
    fx = np.clip(x - np.floor(x), 0, 1)
    fy = np.clip(y - np.floor(y), 0, 1)
    a = atlas_top_down
    top = a[y0, x0] * (1 - fx) + a[y0, x1] * fx
    bottom = a[y1, x0] * (1 - fx) + a[y1, x1] * fx
    return (top * (1 - fy) + bottom * fy) / 255.0


def draw_text(tile, quads, to_px, atlas, color):
    """Rasterizes each glyph quad by sampling the atlas at destination pixel centres, as a GPU would."""
    pixels = np.asarray(tile, dtype=np.float32).copy()
    ink = np.array(color, dtype=np.float32)
    for q in quads:
        left, top = to_px(q["x0"], q["z1"])
        right, bottom = to_px(q["x1"], q["z0"])
        xs = np.arange(int(np.floor(left)), int(np.ceil(right)))
        ys = np.arange(int(np.floor(top)), int(np.ceil(bottom)))
        xs = xs[(xs >= 0) & (xs < pixels.shape[1])]
        ys = ys[(ys >= 0) & (ys < pixels.shape[0])]
        if len(xs) == 0 or len(ys) == 0:
            continue
        cx = xs + 0.5
        cy = ys + 0.5
        inside_x = (cx >= left) & (cx <= right)
        inside_y = (cy >= top) & (cy <= bottom)
        xs, cx = xs[inside_x], cx[inside_x]
        ys, cy = ys[inside_y], cy[inside_y]
        if len(xs) == 0 or len(ys) == 0:
            continue
        tu = (cx - left) / (right - left)
        tv = (cy - top) / (bottom - top)       # 0 at the top edge of the quad
        u = q["u0"] + (q["u1"] - q["u0"]) * tu
        v = q["vTop"] + (q["vBottom"] - q["vTop"]) * tv
        uu, vv = np.meshgrid(u, v)
        alpha = sample_bilinear(atlas, uu, vv)[..., None]
        region = pixels[ys[0]:ys[-1] + 1, xs[0]:xs[-1] + 1, :3]
        pixels[ys[0]:ys[-1] + 1, xs[0]:xs[-1] + 1, :3] = region * (1 - alpha) + ink * alpha
    return Image.fromarray(pixels.clip(0, 255).astype(np.uint8), "RGBA")


def draw_case(case, result, atlas, px, cells, label_font, show_panel=True):
    g = GRAPHICS[case["def"]]
    rot = case["rotation"]
    size_px = int(round(cells * px))
    tile = Image.new("RGBA", (size_px, size_px), (86, 104, 66, 255))
    draw = ImageDraw.Draw(tile)
    for i in range(int(cells) + 1):
        draw.line([(i * px, 0), (i * px, size_px)], fill=(76, 92, 58))
        draw.line([(0, i * px), (size_px, i * px)], fill=(76, 92, 58))
    cx, cy = size_px / 2, size_px / 2   # thing TrueCenter

    def to_px(x, z):
        return cx + x * px, cy - z * px

    if g["offsets"]:
        dx, dz = [(0, 1), (1, 0), (0, -1), (-1, 0)][rot]
        wx, wy = to_px(dx - 0.5, dz + 0.5)
        draw.rectangle([wx, wy, wx + px - 1, wy + px - 1], fill=(120, 116, 110), outline=(60, 58, 55))

    color = STEEL if rot % 2 or g.get("metal") else WOOD
    if g["multi"]:
        name = g["tex"] + ("_east" if rot in (1, 3) else "_north")
        sprite = tinted(name, name + "m", color)
        if rot == 3:
            sprite = sprite.transpose(Image.FLIP_LEFT_RIGHT)
        size = g["size"] if rot in (0, 2) else g["size"][::-1]
    else:
        sprite = tinted(g["tex"], g["tex"] + "_m", color)
        size = g["size"]
    off = g["offsets"][rot] if g["offsets"] else (0, 0)
    sprite = sprite.resize((max(1, int(round(size[0] * px))), max(1, int(round(size[1] * px)))),
                           Image.NEAREST if px >= 64 else Image.BILINEAR)
    sx, sy = to_px(off[0] - size[0] / 2, off[1] + size[1] / 2)
    tile.alpha_composite(sprite, (int(round(sx)), int(round(sy))))

    if show_panel:
        p = result["panel"]
        px0, py0 = to_px(p["x"] - p["w"] / 2, p["z"] + p["h"] / 2)
        px1, py1 = to_px(p["x"] + p["w"] / 2, p["z"] - p["h"] / 2)
        draw.rectangle([px0, py0, px1, py1], outline=(220, 40, 40))

    text_color = case.get("color") or g.get("text") or TEXT
    tile = draw_text(tile, result["quads"], to_px, atlas, text_color)
    if label_font is not None:
        wrap = "" if case.get("autoWrap", True) else " wrap=off"
        label = f'{case["def"].replace("SimpleSigh_", "")} rot={"NESW"[rot]} size={int(case.get("scale", 1) * 100)}%{wrap}'
        ImageDraw.Draw(tile).text((6, 6), label, fill=(255, 255, 255), font=label_font)
    return tile


def run_layout(cases):
    with tempfile.TemporaryDirectory() as tmp:
        cases_path = os.path.join(tmp, "cases.json")
        result_path = os.path.join(tmp, "result.json")
        atlas_path = os.path.join(tmp, "atlas.bin")
        with open(cases_path, "w", encoding="utf-8") as h:
            json.dump(cases, h, ensure_ascii=False)
        subprocess.run(["dotnet", "run", "--project", os.path.join(ROOT, "tools", "preview"), "--",
                        ROOT, FONT, cases_path, result_path, atlas_path], check=True)
        with open(result_path, encoding="utf-8") as h:
            result = json.load(h)
        raw = np.fromfile(atlas_path, dtype=np.uint8).astype(np.float32)
    # The atlas is stored bottom row first; flip it so row 0 is the top for array indexing.
    atlas = raw.reshape(result["atlasHeight"], result["atlasWidth"])[::-1]
    return result["cases"], atlas


def main():
    out_path = sys.argv[1]
    label_font = ImageFont.truetype(FONT, 22)

    results, atlas = run_layout(CASES)
    columns = 4
    rows = (len(CASES) + columns - 1) // columns
    tile = TILE_CELLS * PX
    sheet = Image.new("RGBA", (columns * tile, rows * tile), (0, 0, 0, 255))
    for i, (case, res) in enumerate(zip(CASES, results)):
        sheet.alpha_composite(draw_case(case, res, atlas, PX, TILE_CELLS, label_font), ((i % columns) * tile, (i // columns) * tile))
        if not res["fits"] or res["missing"]:
            print(f"warning: {case['def']} rot {case['rotation']} fits={res['fits']} missing={res['missing']}")
    sheet.convert("RGB").resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS).save(out_path)
    print(out_path)

    zoom_cases = [{"def": "SimpleSigh_BasicSign", "rotation": 0, "text": t, "autoWrap": wrap}
                  for t in ZOOM_LABELS for wrap in (False, True)]
    zoom_results, zoom_atlas = run_layout(zoom_cases)
    cells = 1.25
    enlarge = 2
    widths = [int(round(cells * px)) for _, px in ZOOMS]
    header, row_label = 44, 300
    height = header + sum(int(round(cells * ZOOMS[-1][1])) * enlarge + 8 for _ in zoom_cases)
    zoom_sheet = Image.new("RGB", (row_label + sum(w * enlarge + 12 for w in widths), height), (24, 24, 24))
    draw = ImageDraw.Draw(zoom_sheet)
    small = ImageFont.truetype(FONT, 18)
    columns_text = "columns: " + " | ".join(f"{name} ({px:.0f} px/cell)" for name, px in ZOOMS) + ", shown 2x"
    draw.text((8, 12), columns_text, fill=(230, 230, 230), font=small)
    y = header
    for case, res in zip(zoom_cases, zoom_results):
        row_h = int(round(cells * ZOOMS[-1][1])) * enlarge
        line_px = ", ".join(f"{res['lineHeight'] * px:.0f}" for _, px in ZOOMS)
        mode = "auto wrap" if case["autoWrap"] else "one row"
        draw.text((8, y + 4), f"{case['text']}", fill=(240, 240, 240), font=small)
        draw.text((8, y + 28), f"{mode}: {res['lines']} row(s)", fill=(180, 180, 180), font=small)
        draw.text((8, y + 52), f"row height px: {line_px}", fill=(180, 180, 180), font=small)
        x = row_label
        for (_, px), w in zip(ZOOMS, widths):
            t = draw_case(case, res, zoom_atlas, px, cells, None, show_panel=False).convert("RGB")
            zoom_sheet.paste(t.resize((w * enlarge, w * enlarge), Image.NEAREST), (x, y))
            x += w * enlarge + 12
        y += row_h + 8
    zoom_path = os.path.splitext(out_path)[0] + "-zoom.png"
    zoom_sheet.save(zoom_path)
    print(zoom_path)


if __name__ == "__main__":
    main()
