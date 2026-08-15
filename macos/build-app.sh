#!/bin/bash
# Assembles Wording.app from the executable built by SwiftPM.
#
# The bundle is required, not cosmetic: UNUserNotificationCenter traps in a bare
# executable because there is no bundle identifier. Only a bundled, signed app gets
# its own entry under Settings -> Notifications and can display anything at all.
#
# Usage:
#   ./build-app.sh                          local build, ad-hoc signature
#   VERSION=2026.8.0 ./build-app.sh         stamp a specific version
#   SIGN_IDENTITY="Developer ID Application: Name (TEAMID)" ./build-app.sh
#                                           real signature with hardened runtime
#
# Environment:
#   VERSION        version string written into Info.plist (default: 0.0.0-dev)
#   SIGN_IDENTITY  codesign identity; empty means ad-hoc ("-")
#   CONFIGURATION  swift build configuration (default: release)
#   UNIVERSAL      1 to build arm64 + x86_64 (default in CI, off locally for speed)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${CONFIGURATION:-release}"
VERSION="${VERSION:-0.0.0-dev}"
SIGN_IDENTITY="${SIGN_IDENTITY:-}"
UNIVERSAL="${UNIVERSAL:-0}"
APP="$SCRIPT_DIR/build/Wording.app"

BUILD_FLAGS=(-c "$CONFIGURATION" --package-path "$SCRIPT_DIR")

if [ "$UNIVERSAL" = "1" ]; then
    # Intel Macs are still around and the extra slice is cheap.
    BUILD_FLAGS+=(--arch arm64 --arch x86_64)
fi

echo "==> Building ($CONFIGURATION, version $VERSION, universal=$UNIVERSAL)"
swift build "${BUILD_FLAGS[@]}"

BIN="$(swift build "${BUILD_FLAGS[@]}" --show-bin-path)/WordingApp"

echo "==> Assembling the bundle"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

cp "$BIN" "$APP/Contents/MacOS/Wording"

# No target declares resources any more, so SwiftPM produces no resource bundle and
# there is nothing else to copy. If one is ever added back, it has to be copied in
# here as well: Bundle.module calls fatalError when its bundle is missing, and that
# only shows up on a machine with no words.json - every new user, no developer.

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>Wording</string>
    <key>CFBundleDisplayName</key>
    <string>Wording</string>
    <key>CFBundleIdentifier</key>
    <string>com.lkuklis.wording</string>
    <key>CFBundleExecutable</key>
    <string>Wording</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>14.0</string>
    <!-- The app lives only in the menu bar - no Dock icon. -->
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
PLIST

if [ -n "$SIGN_IDENTITY" ]; then
    echo "==> Signing with Developer ID (hardened runtime)"
    # --options runtime is mandatory for notarisation; --timestamp makes the
    # signature outlive the certificate.
    codesign --force --deep --options runtime --timestamp \
        --sign "$SIGN_IDENTITY" "$APP"
    codesign --verify --strict --verbose=2 "$APP"
else
    echo "==> Signing (ad-hoc - local builds only, Gatekeeper will refuse a download)"
    # Without a signature macOS does not assign a stable identity, so notification
    # permission would be lost on every launch.
    codesign --force --deep --sign - "$APP" >/dev/null 2>&1
fi

echo "==> Done: $APP"
echo "    run with: open \"$APP\""
