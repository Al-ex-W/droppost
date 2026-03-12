# DropPost Icon Generator
# ========================
# Run this script after placing a source PNG (at least 1024x1024) in this directory.
#
# Usage:
#   1. Place your source icon as: src-tauri/icons/app-icon.png  (1024x1024 or larger)
#   2. From the tray/ directory, run:
#        npm run tauri icon src-tauri/icons/app-icon.png
#
# This will generate all required icon variants:
#   - 32x32.png
#   - 128x128.png
#   - 128x128@2x.png
#   - icon.icns  (macOS)
#   - icon.ico   (Windows)
#
# Alternatively, you can use any online tool to create:
#   - A 32x32 PNG  → save as 32x32.png
#   - A 128x128 PNG → save as 128x128.png
#   - A 256x256 PNG → save as 128x128@2x.png
#   - An ICO file (multi-size: 16,32,48,256) → save as icon.ico
#   - An ICNS file → save as icon.icns
#
# Quick placeholder generation (requires ImageMagick installed):
#   magick -size 32x32 xc:#0078d4 32x32.png
#   magick -size 128x128 xc:#0078d4 128x128.png
#   magick -size 256x256 xc:#0078d4 "128x128@2x.png"
#   magick -size 256x256 xc:#0078d4 icon.png
#   magick icon.png icon.ico

Write-Host "To generate icons, run from the tray/ directory:"
Write-Host "  npm run tauri icon src-tauri/icons/app-icon.png"
Write-Host ""
Write-Host "Place a 1024x1024 PNG at src-tauri/icons/app-icon.png first."
