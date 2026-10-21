"""Offline layout preview for Simple Sigh (NOT an in-game screenshot).

1. Measures glyph metrics from a local Japanese font with Pillow.
2. Runs tools/preview (the mod's own SignPanelGeometry + SignTextLayout, linked sources) for every
   sign def and rotation.
3. Composites the result onto the mod's textures. Sprite placement here is written independently
   from the C# code, following RimWorld's rules (TrueCenter, Graphic.DrawOffset, Graphic_Multi
   east/west draw-size swap and west mirroring), so a geometry bug shows up as text off the panel.

Usage: python3 tools/preview/render_preview.py OUTPUT.png [FONT.ttf]
Requires: Pillow, .NET SDK 10.
"""

import json
import os
import subprocess
import sys
import tempfile

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEX = os.path.join(ROOT, "Textures", "Things", "Building")
PX = 128            # pixels per cell
TILE = 4 * PX       # each case is drawn on a 4x4 cell tile
FONT_SIZE = 96
WOOD = (133, 97, 67)
STEEL = (110, 115, 125)
TEXT = (34, 28, 23)

NOTICE = "本日の作業予定：\n午前は畑の収穫、午後は倉庫の整理。夜間は発電設備の点検を行います。"
CASES = [
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "研究室"},
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "発電設備 24h"},
    {"def": "SimpleSigh_BasicSign", "rotation": 0, "text": "Wood A-12", "scale": 0.5},
    {"def": "SimpleSigh_WallSign", "rotation": 0, "text": "倉庫 North"},
    {"def": "SimpleSigh_WallSign", "rotation": 1, "text": "倉庫 East"},
    {"def": "SimpleSigh_WallSign", "rotation": 2, "text": "倉庫 South"},
    {"def": "SimpleSigh_WallSign", "rotation": 3, "text": "倉庫 West"},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 0, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 1, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 2, "text": "倉庫\n備品"},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 3, "text": NOTICE},
    {"def": "SimpleSigh_NoticeBoard", "rotation": 0, "text": NOTICE, "scale": 0.5},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "発電所 稼働中\nPOWER 24h"},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 1, "text": "発電所 稼働中\nPOWER 24h"},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "停電中は消灯（文字は保持）", "color": (90, 230, 120)},
    {"def": "SimpleSigh_ElectronicSign", "rotation": 0, "text": "(power off: text hidden)", "unpowered": True},
]
GRAPHICS = {
    "SimpleSigh_BasicSign": {"tex": "SimpleSign", "multi": False, "size": (1, 1), "footprint": (1, 1), "offsets": None},
    "SimpleSigh_WallSign": {"tex": "SimpleWallSign", "multi": False, "size": (1, 1), "footprint": (1, 1),
                            "offsets": [(0, 0.9), (0.9, 0), (0, -0.9), (-0.9, 0)]},
    "SimpleSigh_NoticeBoard": {"tex": "SimpleNoticeBoard", "multi": True, "size": (2, 1), "footprint": (2, 1), "offsets": None},
    "SimpleSigh_ElectronicSign": {"tex": "SimpleElectronicSign", "multi": True, "size": (2, 1), "footprint": (2, 1), "offsets": None,
                                  "metal": True, "text": (255, 171, 61)},
}


def glyph_metrics(font, chars):
    ascent, descent = font.getmetrics()
    glyphs = {}
    for c in sorted(set(chars) | {"?"}):
        x0, y0, x1, y1 = font.getbbox(c, anchor="la")
        glyphs[c] = {"advance": font.getlength(c), "x0": x0, "y0": y0, "x1": x1, "y1": y1,
                     "visible": c.strip() != "" and x1 > x0 and y1 > y0}
    return {"lineHeight": ascent + descent, "glyphs": glyphs}


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


def draw_case(case, result, font):
    g = GRAPHICS[case["def"]]
    rot = case["rotation"]
    tile = Image.new("RGBA", (TILE, TILE), (86, 104, 66, 255))
    draw = ImageDraw.Draw(tile)
    for i in range(0, TILE, PX):
        draw.line([(i, 0), (i, TILE)], fill=(76, 92, 58))
        draw.line([(0, i), (TILE, i)], fill=(76, 92, 58))
    cx, cy = TILE // 2, TILE // 2   # thing TrueCenter

    def to_px(x, z):
        return cx + x * PX, cy - z * PX

    footprint = g["footprint"] if rot in (0, 2) else g["footprint"][::-1]
    fx0, fy0 = to_px(-footprint[0] / 2, footprint[1] / 2)
    draw.rectangle([fx0, fy0, fx0 + footprint[0] * PX - 1, fy0 + footprint[1] * PX - 1], outline=(200, 200, 120))
    if g["offsets"]:
        dx, dz = [(0, 1), (1, 0), (0, -1), (-1, 0)][rot]
        wx, wy = to_px(dx - 0.5, dz + 0.5)
        draw.rectangle([wx, wy, wx + PX - 1, wy + PX - 1], fill=(120, 116, 110), outline=(60, 58, 55))

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
    sprite = sprite.resize((int(size[0] * PX), int(size[1] * PX)), Image.NEAREST)
    sx, sy = to_px(off[0] - size[0] / 2, off[1] + size[1] / 2)
    tile.alpha_composite(sprite, (int(round(sx)), int(round(sy))))

    p = result["panel"]
    px0, py0 = to_px(p["x"] - p["w"] / 2, p["z"] + p["h"] / 2)
    px1, py1 = to_px(p["x"] + p["w"] / 2, p["z"] - p["h"] / 2)
    draw.rectangle([px0, py0, px1, py1], outline=(220, 40, 40))

    text_color = case.get("color") or g.get("text") or TEXT
    for q in ([] if case.get("unpowered") else result["quads"]):
        x0, y0 = to_px(q["x0"], q["z1"])
        x1, y1 = to_px(q["x1"], q["z0"])
        w, h = max(1, int(round(x1 - x0))), max(1, int(round(y1 - y0)))
        bx0, by0, bx1, by1 = font.getbbox(q["ch"], anchor="la")
        glyph = Image.new("L", (bx1 - bx0, by1 - by0), 0)
        ImageDraw.Draw(glyph).text((-bx0, -by0), q["ch"], font=font, fill=255, anchor="la")
        glyph = glyph.resize((w, h), Image.LANCZOS)
        ink = Image.new("RGBA", (w, h), tuple(text_color) + (255,))
        tile.paste(ink, (int(round(x0)), int(round(y0))), glyph)

    label = f'{case["def"].replace("SimpleSigh_", "")} rot={"NESW"[rot]}' + (f' size={int(case.get("scale", 1) * 100)}%')
    draw.text((6, 6), label, fill=(255, 255, 255), font=ImageFont.truetype(font.path, 22))
    return tile


def main():
    out_path = sys.argv[1]
    font_path = sys.argv[2] if len(sys.argv) > 2 else "/usr/share/fonts/opentype/ipafont-gothic/ipag.ttf"
    font = ImageFont.truetype(font_path, FONT_SIZE)
    with tempfile.TemporaryDirectory() as tmp:
        metrics = os.path.join(tmp, "metrics.json")
        cases = os.path.join(tmp, "cases.json")
        result = os.path.join(tmp, "result.json")
        with open(metrics, "w", encoding="utf-8") as h:
            json.dump(glyph_metrics(font, "".join(c["text"] for c in CASES)), h, ensure_ascii=False)
        with open(cases, "w", encoding="utf-8") as h:
            json.dump(CASES, h, ensure_ascii=False)
        subprocess.run(["dotnet", "run", "--project", os.path.join(ROOT, "tools", "preview"), "--",
                        ROOT, metrics, cases, result], check=True)
        with open(result, encoding="utf-8") as h:
            results = json.load(h)

    columns = 4
    rows = (len(CASES) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * TILE, rows * TILE), (0, 0, 0, 255))
    for i, (case, res) in enumerate(zip(CASES, results)):
        sheet.alpha_composite(draw_case(case, res, font), ((i % columns) * TILE, (i // columns) * TILE))
        if not res["fits"]:
            print(f"warning: {case['def']} rot {case['rotation']} does not fit")
    sheet.convert("RGB").resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS).save(out_path)
    print(out_path)


if __name__ == "__main__":
    main()
