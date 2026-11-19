#!/bin/sh
# Copyright (c) 2026 Luke Harrison
# SPDX-License-Identifier: MIT
# Rebuilds the branding: SVGs, PNG sizes and assets/app.ico.
# Needs Python 3 with fontTools and Pillow, rsvg-convert (librsvg) and the Inter font.
set -e
cd "$(dirname "$0")"
python3 make_branding.py
mkdir -p png
for s in 16 20 24 32; do
  rsvg-convert -w $s -h $s icon-small.svg -o png/icon-$s.png
done
for s in 40 48 64 128 256 512 1024; do
  rsvg-convert -w $s -h $s icon.svg -o png/icon-$s.png
done
python3 - <<'PY'
from PIL import Image
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
imgs = [Image.open(f"png/icon-{s}.png").convert("RGBA") for s in sizes]
imgs[-1].save("../app.ico", sizes=[(s, s) for s in sizes], append_images=imgs[:-1])
print("Wrote assets/app.ico")
PY
rsvg-convert -h 192 logo.svg -o png/logo-192.png
rsvg-convert -h 192 logo-dark.svg -o png/logo-dark-192.png
echo "Done."
