#!/bin/bash
# Sklada Wording.app z pliku wykonywalnego zbudowanego przez SwiftPM.
#
# Pakiet jest konieczny, a nie kosmetyczny: UNUserNotificationCenter przewraca
# sie w golym pliku wykonywalnym, bo nie ma identyfikatora pakietu. Dopiero
# zapakowana i podpisana aplikacja dostaje wlasny wpis w Ustawieniach ->
# Powiadomienia i moze cokolwiek wyswietlic.
set -euo pipefail

KATALOG="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KONFIGURACJA="${1:-release}"
APP="$KATALOG/build/Wording.app"

echo "==> Kompilacja ($KONFIGURACJA)"
swift build -c "$KONFIGURACJA" --package-path "$KATALOG"

BIN="$(swift build -c "$KONFIGURACJA" --package-path "$KATALOG" --show-bin-path)/WordingApp"

echo "==> Skladanie pakietu"
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
    <!-- Aplikacja zyje wylacznie w pasku menu - bez ikony w Docku. -->
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
PLIST

echo "==> Podpisywanie (ad-hoc)"
# Bez podpisu macOS nie przydziela stabilnej tozsamosci, wiec zgoda na
# powiadomienia gubilaby sie przy kazdym uruchomieniu.
codesign --force --sign - "$APP" >/dev/null 2>&1

echo "==> Gotowe: $APP"
echo "    uruchom:  open \"$APP\""
