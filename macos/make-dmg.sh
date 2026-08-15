#!/bin/bash
# Packs Wording.app into a .dmg with the usual drag-to-Applications layout.
#
# Usage: VERSION=2026.8.0 ./make-dmg.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${VERSION:-0.0.0-dev}"
APP="$SCRIPT_DIR/build/Wording.app"
STAGING="$SCRIPT_DIR/build/dmg"
DMG="$SCRIPT_DIR/build/Wording-$VERSION-macos-universal.dmg"

if [ ! -d "$APP" ]; then
    echo "error: $APP not found - run build-app.sh first" >&2
    exit 1
fi

echo "==> Staging"
rm -rf "$STAGING" "$DMG"
mkdir -p "$STAGING"
cp -R "$APP" "$STAGING/"
ln -s /Applications "$STAGING/Applications"

echo "==> Creating $DMG"
hdiutil create \
    -volname "Wording $VERSION" \
    -srcfolder "$STAGING" \
    -ov -format UDZO \
    "$DMG" >/dev/null

rm -rf "$STAGING"

echo "==> Done: $DMG"
