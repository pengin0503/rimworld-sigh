#!/usr/bin/env python3
"""Regenerates fixture.ttf: a tiny TrueType font with known geometry for the font parser tests.

    pip install fonttools && python3 make_fixture_font.py

unitsPerEm 1000, ascender 800, descender -200, cmap format 4 only.
  'A' (U+0041) square: one contour (100,0)-(500,0)-(500,400)-(100,400), advance 600
  'B' (U+0042) composite: 'A' scaled 0.5 and moved by (+50,+100), advance 400
  'O' (U+004F) ring: outer square 0..600 with a counter-wound inner square 200..400, advance 700
  'Q' (U+0051) one quadratic arc: on (0,0), off (300,600), on (600,0), advance 700
  ' ' (U+0020) no outline, advance 250
"""
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib.tables._g_l_y_f import GlyphComponent
from pathlib import Path


def square(pen, x0, y0, x1, y1, clockwise=True):
    pts = [(x0, y0), (x0, y1), (x1, y1), (x1, y0)] if clockwise else [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    pen.moveTo(pts[0])
    for p in pts[1:]:
        pen.lineTo(p)
    pen.closePath()


def main():
    order = [".notdef", "space", "A", "B", "O", "Q"]
    fb = FontBuilder(1000, isTTF=True)
    fb.setupGlyphOrder(order)
    fb.setupCharacterMap({0x20: "space", 0x41: "A", 0x42: "B", 0x4F: "O", 0x51: "Q"})
    glyphs = {}
    pen = TTGlyphPen(None); square(pen, 50, 0, 450, 700); glyphs[".notdef"] = pen.glyph()
    glyphs["space"] = TTGlyphPen(None).glyph()
    pen = TTGlyphPen(None); square(pen, 100, 0, 500, 400); glyphs["A"] = pen.glyph()
    pen = TTGlyphPen(None); square(pen, 0, 0, 600, 600); square(pen, 200, 200, 400, 400, clockwise=False); glyphs["O"] = pen.glyph()
    pen = TTGlyphPen(None); pen.moveTo((0, 0)); pen.qCurveTo((300, 600), (600, 0)); pen.closePath(); glyphs["Q"] = pen.glyph()
    pen = TTGlyphPen(glyphs); pen.addComponent("A", (0.5, 0, 0, 0.5, 50, 100)); glyphs["B"] = pen.glyph()
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics({".notdef": (500, 50), "space": (250, 0), "A": (600, 100), "B": (400, 100), "O": (700, 0), "Q": (700, 0)})
    fb.setupHorizontalHeader(ascent=800, descent=-200)
    fb.setupNameTable({"familyName": "SimpleSighFixture", "styleName": "Regular"})
    fb.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
    fb.setupPost()
    fb.font["cmap"].tables = [t for t in fb.font["cmap"].tables if t.format == 4]
    fb.save(str(Path(__file__).with_name("fixture.ttf")))


if __name__ == "__main__":
    main()
