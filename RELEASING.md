# Releasing

## Cutting a release

Versions are **CalVer**: `YYYY.M.PATCH`, for example `2026.8.0`, then `2026.8.1` for a
fix and `2026.9.0` for the next month's release.

The git tag is the single source of truth — nothing in the repository hardcodes a
version number. Tagging is the whole release process:

```bash
git tag v2026.8.0
git push origin v2026.8.0
```

That triggers `.github/workflows/release.yml`, which builds both platforms in parallel
and publishes a GitHub Release with:

| Asset | What it is |
|---|---|
| `Wording-<version>-win-x64-setup.exe` | Windows installer (Inno Setup) |
| `Wording-<version>-win-x64-portable.zip` | the same build, no installer |
| `Wording-<version>-macos-universal.dmg` | macOS disk image, arm64 + x86_64 |
| `checksums.txt` | SHA-256 of every asset |

To rehearse without publishing anything, run the workflow manually from the Actions tab
(**Run workflow** → enter a version). It builds and uploads artifacts but creates no
release.

## Required secrets

Set these under **Settings → Secrets and variables → Actions**. The workflow runs
without them, but the macOS build then ships ad-hoc signed and **Gatekeeper will refuse
to open it** on any machine that downloaded it.

| Secret | Where it comes from |
|---|---|
| `MACOS_CERTIFICATE` | base64 of the exported Developer ID `.p12` |
| `MACOS_CERTIFICATE_PWD` | the password you set when exporting the `.p12` |
| `MACOS_SIGN_IDENTITY` | e.g. `Developer ID Application: Jan Kowalski (AB12CD34EF)` |
| `APPLE_ID` | the Apple ID e-mail of the developer account |
| `APPLE_APP_PASSWORD` | an app-specific password, **not** the account password |
| `APPLE_TEAM_ID` | the 10-character team id |

### Getting them

1. **Join the Apple Developer Program** ($99/year). Notarisation is not available on a
   free account.

2. **Create a Developer ID Application certificate.** In Xcode: *Settings → Accounts →
   Manage Certificates → + → Developer ID Application*. It lands in your login keychain.

3. **Export it.** In Keychain Access find the certificate, expand it so the private key
   is included, right-click → *Export* → `.p12`, and set a password. Then:

   ```bash
   base64 -i Certificates.p12 | pbcopy    # paste as MACOS_CERTIFICATE
   ```

4. **Read the identity name** — this is the exact string the workflow signs with:

   ```bash
   security find-identity -v -p codesigning
   ```

5. **Create an app-specific password** at <https://appleid.apple.com> → *Sign-In and
   Security → App-Specific Passwords*. This is what `notarytool` authenticates with.

6. **Find the team id** at <https://developer.apple.com/account> under *Membership*.

## Windows and SmartScreen

The Windows installer is **not** code signed. On first run SmartScreen shows
*"Windows protected your PC"*; the user has to click **More info → Run anyway**. The
warning fades once enough people have installed the same binary, but it never fully
disappears without a certificate.

Removing it costs money: Azure Trusted Signing (roughly $10/month, requires
organisation validation) or an OV certificate from DigiCert or Sectigo (roughly
$200–400/year). When you get one, add signing to the `windows` job — the rest of the
pipeline does not change.

## Verifying a release locally

```bash
# macOS: confirm the disk image is notarised and stapled
xcrun stapler validate Wording-2026.8.0-macos-universal.dmg
spctl -a -vvv -t install /Volumes/Wording\ 2026.8.0/Wording.app
```

`spctl` should report `source=Notarized Developer ID`. If it says `rejected`, the build
went out unsigned or notarisation failed.

## Local builds

```bash
VERSION=2026.8.0 ./macos/build-app.sh          # ad-hoc signed, for local use
VERSION=2026.8.0 ./macos/make-dmg.sh
UNIVERSAL=1 VERSION=2026.8.0 ./macos/build-app.sh   # both architectures
```

An ad-hoc signed build runs fine on the machine that built it. It will not survive
being downloaded from anywhere, because the quarantine attribute plus a missing
Developer ID signature is exactly what Gatekeeper blocks.
