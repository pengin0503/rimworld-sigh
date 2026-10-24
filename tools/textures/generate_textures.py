"""Generate Simple Sigh's original building textures.

All art is drawn procedurally here so the repository never contains copied
RimWorld textures. Each building gets a main texture (light grey, tinted by
the stuff colour) and a CutoutComplex mask (`_m`). In the 1.7 CutoutComplex
shader the mask's red channel selects where the stuff colour is applied; the
black display panel therefore keeps its own light colour so text stays legible
on dark materials.

Run: python3 tools/textures/generate_textures.py
"""

import os
import struct
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Textures", "Things", "Building")

OUTLINE = (38, 34, 30, 255)
FRAME = (214, 214, 214, 255)
FRAME_LIGHT = (240, 240, 240, 255)
FRAME_DARK = (168, 168, 168, 255)
GRAIN = (196, 196, 196, 255)
PANEL = (236, 228, 206, 255)
PANEL_SHADE = (214, 204, 178, 255)
NAIL = (120, 116, 108, 255)
TINT = (255, 0, 0, 255)
NO_TINT = (0, 0, 0, 255)
CLEAR = (0, 0, 0, 0)


class Canvas:
    def __init__(self, width, height, fill=CLEAR):
        self.width = width
        self.height = height
        self.pixels = [list(fill) for _ in range(width * height)]

    def rect(self, x0, y0, x1, y1, color):
        """Fill [x0, x1) x [y0, y1) in top-left pixel coordinates."""
        for y in range(max(0, y0), min(self.height, y1)):
            for x in range(max(0, x0), min(self.width, x1)):
                self.pixels[y * self.width + x] = list(color)

    def frame(self, x0, y0, x1, y1, thickness, color):
        self.rect(x0, y0, x1, y0 + thickness, color)
        self.rect(x0, y1 - thickness, x1, y1, color)
        self.rect(x0, y0, x0 + thickness, y1, color)
        self.rect(x1 - thickness, y0, x1, y1, color)

    def save(self, file):
        raw = bytearray()
        for y in range(self.height):
            raw.append(0)
            for x in range(self.width):
                raw.extend(self.pixels[y * self.width + x])

        def chunk(kind, data):
            body = kind + data
            return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

        header = struct.pack(">IIBBBBB", self.width, self.height, 8, 6, 0, 0, 0)
        data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
        os.makedirs(os.path.dirname(file), exist_ok=True)
        with open(file, "wb") as handle:
            handle.write(data)


def board(tex, mask, x0, y0, x1, y1, border, panel_inset):
    """Draw an outlined, bevelled board with a light inner display panel.

    Returns the panel rectangle in pixel coordinates (x0, y0, x1, y1).
    """
    tex.rect(x0, y0, x1, y1, OUTLINE)
    mask.rect(x0, y0, x1, y1, NO_TINT)
    tex.rect(x0 + border, y0 + border, x1 - border, y1 - border, FRAME)
    mask.rect(x0 + border, y0 + border, x1 - border, y1 - border, TINT)
    # Bevel: light top/left, dark bottom/right, like vanilla furniture edges.
    tex.rect(x0 + border, y0 + border, x1 - border, y0 + border + 2, FRAME_LIGHT)
    tex.rect(x0 + border, y0 + border, x0 + border + 2, y1 - border, FRAME_LIGHT)
    tex.rect(x0 + border, y1 - border - 3, x1 - border, y1 - border, FRAME_DARK)
    tex.rect(x1 - border - 3, y0 + border, x1 - border, y1 - border, FRAME_DARK)
    # A little grain on the frame so wood/stone/metal tints read as material.
    for gy in range(y0 + border + 5, y1 - border - 4, 6):
        tex.rect(x0 + border + 3, gy, x0 + border + panel_inset - 3, gy + 1, GRAIN)
        tex.rect(x1 - border - panel_inset + 3, gy, x1 - border - 3, gy + 1, GRAIN)

    px0, py0, px1, py1 = x0 + panel_inset, y0 + panel_inset, x1 - panel_inset, y1 - panel_inset
    tex.rect(px0 - 2, py0 - 2, px1 + 2, py1 + 2, OUTLINE)
    mask.rect(px0 - 2, py0 - 2, px1 + 2, py1 + 2, NO_TINT)
    tex.rect(px0, py0, px1, py1, PANEL)
    tex.rect(px0, py0, px1, py0 + 3, PANEL_SHADE)
    tex.rect(px0, py0, px0 + 3, py1, PANEL_SHADE)
    # Corner nails sit on the frame, outside the text panel.
    for nx, ny in ((x0 + border + 2, y0 + border + 2), (x1 - border - 6, y0 + border + 2),
                   (x0 + border + 2, y1 - border - 6), (x1 - border - 6, y1 - border - 6)):
        tex.rect(nx, ny, nx + 4, ny + 4, NAIL)
        mask.rect(nx, ny, nx + 4, ny + 4, NO_TINT)
    return px0, py0, px1, py1


def post(tex, mask, x0, y0, x1, y1):
    tex.rect(x0, y0, x1, y1, OUTLINE)
    tex.rect(x0 + 3, y0, x1 - 3, y1 - 3, FRAME)
    tex.rect(x0 + 3, y0, x0 + 5, y1 - 3, FRAME_LIGHT)
    tex.rect(x1 - 6, y0, x1 - 3, y1 - 3, FRAME_DARK)
    mask.rect(x0 + 3, y0, x1 - 3, y1 - 3, TINT)


SCREEN = (16, 18, 20, 255)
SCREEN_DOT = (34, 38, 42, 255)
LED_GREEN = (70, 220, 90, 255)


def led_board(tex, mask, x0, y0, x1, y1, border, panel_inset):
    """Metal-framed LED panel: tinted frame, dark untinted screen with a faint dot matrix."""
    px0, py0, px1, py1 = board(tex, mask, x0, y0, x1, y1, border, panel_inset)
    tex.rect(px0, py0, px1, py1, SCREEN)
    for dy in range(py0 + 2, py1 - 1, 4):
        for dx in range(px0 + 2, px1 - 1, 4):
            tex.rect(dx, dy, dx + 2, dy + 2, SCREEN_DOT)
    # Status LED on the frame, bottom right, kept untinted.
    lx, ly = x1 - border - 12, y1 - border - 8
    tex.rect(lx, ly, lx + 5, ly + 4, LED_GREEN)
    mask.rect(lx, ly, lx + 5, ly + 4, NO_TINT)
    return px0, py0, px1, py1


FLOOR_PAINT = (232, 228, 216, 255)


def floor_marking(tex, x0, y0, x1, y1, arm=22, width=4, inset=10):
    """Painted corner brackets on the floor; the inside stays transparent so text sits on the floor itself."""
    corners = ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1))
    for layer, grow in ((OUTLINE, 1), (FLOOR_PAINT, 0)):
        for cx, cy, dx, dy in corners:
            # Horizontal and vertical arm of an L, drawn from the corner inwards.
            hx0, hx1 = sorted((cx, cx + dx * arm))
            hy0, hy1 = sorted((cy, cy + dy * width))
            vx0, vx1 = sorted((cx, cx + dx * width))
            vy0, vy1 = sorted((cy, cy + dy * arm))
            tex.rect(hx0 - grow, hy0 - grow, hx1 + grow, hy1 + grow, layer)
            tex.rect(vx0 - grow, vy0 - grow, vx1 + grow, vy1 + grow, layer)
    return x0 + inset, y0 + inset, x1 - inset, y1 - inset


def floor_sign(tex):
    return floor_marking(tex, 4, 4, 124, 124)


def floor_sign_landscape3(tex):
    return floor_marking(tex, 4, 4, 380, 124)


def floor_sign_portrait3(tex):
    return floor_marking(tex, 4, 4, 124, 380)


def normalized(panel, width, height):
    """Convert a top-left pixel rect to RimWorld-style (x, y, w, h) from the bottom-left."""
    x0, y0, x1, y1 = panel
    return (round(x0 / width, 4), round((height - y1) / height, 4),
            round((x1 - x0) / width, 4), round((y1 - y0) / height, 4))


def make(name, width, height, draw, mask_suffix="_m"):
    """Graphic_Single masks are `<name>_m`; Graphic_Multi masks are `<name>_<rot>m`."""
    tex = Canvas(width, height)
    mask = Canvas(width, height, NO_TINT)
    panel = draw(tex, mask)
    tex.save(os.path.join(OUT, name + ".png"))
    mask.save(os.path.join(OUT, name + mask_suffix + ".png"))
    return normalized(panel, width, height)


def make_unmasked(name, width, height, draw):
    """Untinted art for the plain Cutout shader (no stuff colour, not paintable): no mask file."""
    tex = Canvas(width, height)
    panel = draw(tex)
    tex.save(os.path.join(OUT, name + ".png"))
    return normalized(panel, width, height)


def basic_sign(tex, mask):
    post(tex, mask, 22, 80, 36, 124)
    post(tex, mask, 92, 80, 106, 124)
    return board(tex, mask, 6, 10, 122, 90, 3, 12)


def wall_sign(tex, mask):
    # Hanging plaque: two short chains above a board that fills the cell width.
    for cx in (30, 94):
        tex.rect(cx, 8, cx + 4, 26, OUTLINE)
        tex.rect(cx + 1, 9, cx + 3, 26, NAIL)
    return board(tex, mask, 4, 24, 124, 108, 3, 12)


def notice_board_landscape(tex, mask):
    post(tex, mask, 34, 92, 50, 126)
    post(tex, mask, 206, 92, 222, 126)
    return board(tex, mask, 6, 6, 250, 102, 3, 12)


def notice_board_portrait(tex, mask):
    post(tex, mask, 22, 216, 38, 254)
    post(tex, mask, 90, 216, 106, 254)
    return board(tex, mask, 6, 8, 122, 226, 3, 12)


def electronic_landscape(tex, mask):
    post(tex, mask, 34, 92, 50, 126)
    post(tex, mask, 206, 92, 222, 126)
    return led_board(tex, mask, 6, 6, 250, 102, 3, 12)


def electronic_portrait(tex, mask):
    post(tex, mask, 22, 216, 38, 254)
    post(tex, mask, 90, 216, 106, 254)
    return led_board(tex, mask, 6, 8, 122, 226, 3, 12)


def notice_board_landscape3(tex, mask):
    post(tex, mask, 52, 92, 68, 126)
    post(tex, mask, 316, 92, 332, 126)
    return board(tex, mask, 6, 6, 378, 102, 3, 12)


def notice_board_portrait3(tex, mask):
    post(tex, mask, 22, 344, 38, 382)
    post(tex, mask, 90, 344, 106, 382)
    return board(tex, mask, 6, 8, 122, 354, 3, 12)


def electronic_landscape3(tex, mask):
    post(tex, mask, 52, 92, 68, 126)
    post(tex, mask, 316, 92, 332, 126)
    return led_board(tex, mask, 6, 6, 378, 102, 3, 12)


def electronic_portrait3(tex, mask):
    post(tex, mask, 22, 344, 38, 382)
    post(tex, mask, 90, 344, 106, 382)
    return led_board(tex, mask, 6, 8, 122, 354, 3, 12)


def sign_list_icon():
    """White silhouette for the minimized main-tab button, like vanilla's UI/Buttons/MainButtons icons."""
    white, clear = (255, 255, 255, 255), CLEAR
    icon = Canvas(64, 64)
    icon.rect(8, 8, 56, 40, white)            # board
    for y in (15, 23, 31):
        icon.rect(14, y, 50 if y != 31 else 40, y + 3, clear)  # text lines cut out of the board
    icon.rect(16, 40, 22, 58, white)           # posts
    icon.rect(42, 40, 48, 58, white)
    file = os.path.join(ROOT, "Textures", "UI", "Buttons", "MainButtons", "SimpleSigh_Signs.png")
    icon.save(file)


def main():
    sign_list_icon()
    rects = {
        "SimpleSign": make("SimpleSign", 128, 128, basic_sign),
        "SimpleWallSign": make("SimpleWallSign", 128, 128, wall_sign),
    }
    # South falls back to north and west mirrors east (Graphic_Multi rules).
    north = make("SimpleNoticeBoard_north", 256, 128, notice_board_landscape, "m")
    east = make("SimpleNoticeBoard_east", 128, 256, notice_board_portrait, "m")
    rects["SimpleNoticeBoard (north/south)"] = north
    rects["SimpleNoticeBoard (east/west)"] = east
    rects["SimpleElectronicSign (north/south)"] = make("SimpleElectronicSign_north", 256, 128, electronic_landscape, "m")
    rects["SimpleElectronicSign (east/west)"] = make("SimpleElectronicSign_east", 128, 256, electronic_portrait, "m")
    rects["SimpleNoticeBoard3 (north/south)"] = make("SimpleNoticeBoard3_north", 384, 128, notice_board_landscape3, "m")
    rects["SimpleNoticeBoard3 (east/west)"] = make("SimpleNoticeBoard3_east", 128, 384, notice_board_portrait3, "m")
    rects["SimpleElectronicSign3 (north/south)"] = make("SimpleElectronicSign3_north", 384, 128, electronic_landscape3, "m")
    rects["SimpleElectronicSign3 (east/west)"] = make("SimpleElectronicSign3_east", 128, 384, electronic_portrait3, "m")
    rects["SimpleFloorSign"] = make_unmasked("SimpleFloorSign", 128, 128, floor_sign)
    rects["SimpleFloorSign3 (north/south)"] = make_unmasked("SimpleFloorSign3_north", 384, 128, floor_sign_landscape3)
    rects["SimpleFloorSign3 (east/west)"] = make_unmasked("SimpleFloorSign3_east", 128, 384, floor_sign_portrait3)
    for key, value in rects.items():
        print(f"{key}: panelRect=({value[0]},{value[1]},{value[2]},{value[3]})")


if __name__ == "__main__":
    main()
