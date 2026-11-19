#!/usr/bin/env python3
# Copyright (c) 2026 Luke Harrison
# SPDX-License-Identifier: MIT
"""Generates the Classroom Toolkit icon, logo and wordmark.

Writes, next to this script:
  icon.svg                 the app icon (traffic light on a blue tile)
  icon-small.svg           the same, simplified for 16 to 32 px
  logo.svg / logo-dark.svg icon + wordmark, for dark / light backgrounds
  wordmark.svg / wordmark-dark.svg
and asks build.sh to render the PNG sizes and assets/app.ico.

The wordmark is set in Inter Display SemiBold and converted to outlines, so the
SVGs look the same everywhere without the font installed. Inter is under the
SIL Open Font License; outlines of it may be used in logos.
"""
import os
import sys

from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

HERE = os.path.dirname(os.path.abspath(__file__))

# Colours shared with the app (src/Ui.cs, Theme).
ACCENT = "#3884ff"
HOUSING = "#10161f"
RED, AMBER, GREEN = "#ef4444", "#f59e0b", "#22c55e"
INK_LIGHT = "#ffffff"   # wordmark on dark backgrounds
INK_DARK = "#12161e"    # wordmark on light backgrounds
MUTED_LIGHT = "#9aa6ba"
MUTED_DARK = "#5d6779"

FONT_PATHS = [
    "/usr/share/fonts/opentype/inter/InterDisplay-SemiBold.otf",
    os.path.join(HERE, "InterDisplay-SemiBold.otf"),
]


def icon_group(x=0, y=0, size=256):
    """The icon drawn in a 256-unit box, scaled to `size` at (x, y)."""
    k = size / 256
    return f'''<g transform="translate({x:g} {y:g}) scale({k:g})">
    <rect x="8" y="8" width="240" height="240" rx="54" fill="{ACCENT}"/>
    <rect x="88" y="28" width="80" height="200" rx="40" fill="{HOUSING}"/>
    <circle cx="128" cy="66" r="26.5" fill="{RED}"/>
    <circle cx="128" cy="128" r="26.5" fill="{AMBER}"/>
    <circle cx="128" cy="190" r="26.5" fill="{GREEN}"/>
  </g>'''


def icon_small():
    """Simplified icon for 16 to 32 px: tile edge to edge, wider housing, bigger lights."""
    return f'''<g>
    <rect x="0" y="0" width="256" height="256" rx="56" fill="{ACCENT}"/>
    <rect x="70" y="12" width="116" height="232" rx="58" fill="{HOUSING}"/>
    <circle cx="128" cy="58" r="36" fill="{RED}"/>
    <circle cx="128" cy="128" r="36" fill="{AMBER}"/>
    <circle cx="128" cy="198" r="36" fill="{GREEN}"/>
  </g>'''


def text_path(text, font, size, x, y):
    """Returns (svg path data, advance width) for text with its baseline at y."""
    gs = font.getGlyphSet()
    cmap = font.getBestCmap()
    upm = font["head"].unitsPerEm
    scale = size / upm
    pen = SVGPathPen(gs)
    cursor = 0.0
    for ch in text:
        name = cmap.get(ord(ch))
        if name is None:
            continue
        glyph = gs[name]
        tp = TransformPen(pen, (scale, 0, 0, -scale, x + cursor * scale, y))
        glyph.draw(tp)
        cursor += glyph.width
    return pen.getCommands(), cursor * scale


def write(name, body, w, h):
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w:g}" height="{h:g}" '
           f'viewBox="0 0 {w:g} {h:g}">\n  {body}\n</svg>\n')
    with open(os.path.join(HERE, name), "w", encoding="utf-8") as f:
        f.write(svg)


def main():
    path = next((p for p in FONT_PATHS if os.path.exists(p)), None)
    if path is None:
        sys.exit("Inter Display SemiBold not found; install the Inter font family.")
    font = TTFont(path)

    write("icon.svg", icon_group(), 256, 256)
    write("icon-small.svg", icon_small(), 256, 256)

    # Wordmark: "Classroom Toolkit" on one line.
    size, base = 64, 62
    d, width = text_path("Classroom Toolkit", font, size, 0, base)
    h = 80
    for suffix, ink in (("", INK_LIGHT), ("-dark", INK_DARK)):
        write(f"wordmark{suffix}.svg", f'<path fill="{ink}" d="{d}"/>', round(width + 2), h)

    # Logo: icon at the left, wordmark beside it, vertically centred.
    icon = 96
    gap = 26
    tx = icon + gap
    d2, w2 = text_path("Classroom Toolkit", font, size, tx, 48 + 22)
    total_w = round(tx + w2 + 4)
    for suffix, ink in (("", INK_LIGHT), ("-dark", INK_DARK)):
        body = icon_group(0, 0, icon) + f'\n  <path fill="{ink}" d="{d2}"/>'
        write(f"logo{suffix}.svg", body, total_w, icon)
    print("Wrote icon.svg, icon-small.svg, wordmark.svg, wordmark-dark.svg, logo.svg, logo-dark.svg")


if __name__ == "__main__":
    main()
