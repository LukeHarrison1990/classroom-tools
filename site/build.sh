#!/bin/sh
# Copyright (c) 2026 Luke Harrison
# SPDX-License-Identifier: MIT
# Assembles the website into _site/ (or the folder given), copying the images
# from docs/ and assets/ so they aren't stored twice. GitHub Actions runs this;
# run it yourself and open _site/index.html to preview the site.
set -e
cd "$(dirname "$0")/.."
out="${1:-_site}"
rm -rf "$out"
mkdir -p "$out/img"
cp site/index.html site/style.css site/script.js "$out/"
cp -r site/fonts "$out/"
cp docs/screenshots/*.png docs/social-preview.png "$out/img/"
cp assets/branding/logo.svg assets/branding/logo-dark.svg assets/branding/icon.svg "$out/img/"
cp assets/branding/png/icon-32.png assets/branding/png/icon-256.png "$out/img/"
touch "$out/.nojekyll"
echo "Website assembled in $out/"
