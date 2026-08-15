#!/bin/bash
# Assembles Wording.app from the executable built by SwiftPM.
#
# The bundle is required, not cosmetic: UNUserNotificationCenter traps in a bare
# executable because there is no bundle identifier. Only a bundled, signed app gets
# its own entry under Settings -> Notifications and can display anything at all.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${1:-release}"
APP="$SCRIPT_DIR/build/Wording.app"

echo "==> Building ($CONFIGURATION)"
swift build -c "$CONFIGURATION" --package-path "$SCRIPT_DIR"

BIN="$(swift build -c "$CONFIGURATION" --package-path "$SCRIPT_DIR" --show-bin-path)/WordingApp"

echo "==> Assembling the bundle"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

cp "$BIN" "$APP/Contents/MacOS/Wording"

cat > "$APP/Contents/Info.plist" <<'PLIST'
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
    <string>2.0</string>
    <key>CFBundleVersion</key>
    <string>2</string>
    <key>LSMinimumSystemVersion</key>
    <string>14.0</string>
    <!-- The app lives only in the menu bar - no Dock icon. -->
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
PLIST

echo "==> Signing (ad-hoc)"
# Without a signature macOS does not assign a stable identity, so notification
# permission would be lost on every launch.
codesign --force --sign - "$APP" >/dev/null 2>&1

echo "==> Done: $APP"
echo "    run with: open \"$APP\""
