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

**All six are already configured** — nothing needs doing for a routine release. This
section is here for when the certificate expires, a key is rotated, or someone sets the
project up on another machine.

Without them the macOS build ships ad-hoc signed and **Gatekeeper refuses to open it**
on any machine that downloaded it.

| Secret | Where it comes from |
|---|---|
| `MACOS_CERTIFICATE` | base64 of the Developer ID `.p12` |
| `MACOS_CERTIFICATE_PWD` | the password used when building the `.p12` |
| `MACOS_SIGN_IDENTITY` | `Developer ID Application: Lukasz Kuklis (5JV3LDX8GV)` |
| `APPLE_API_KEY` | base64 of the App Store Connect API `.p8` |
| `APPLE_API_KEY_ID` | the key id shown next to the key |
| `APPLE_API_ISSUER` | the issuer id shown above the key list |

The signing material lives outside the repository, at `~/.appstoreconnect/` on the
maintainer's machine: the API key under `private_keys/`, plus
`wording-developerid.key`, `wording-developerid.p12` and the `.p12` password. The
Developer ID certificate itself expires **2031-08-16**.

Notarisation authenticates with an App Store Connect API key rather than an Apple ID and
an app-specific password. It is a team credential, it can be revoked on its own, and it
keeps a personal account with 2FA out of CI.

### Creating the Developer ID certificate

**This one step cannot be automated.** `POST /v1/certificates` with
`DEVELOPER_ID_APPLICATION` returns:

```
HTTP 403 - This operation can only be performed by the Account Holder.
```

Account Holder is a role held by a person, not something an API key can carry, so no
amount of raising the key's permissions gets past it. Everything else — reading the
certificate list, notarisation, setting the repository secrets — does work through the
API.

A certificate signing request and its private key are already prepared:

| File | What it is |
|---|---|
| `~/.appstoreconnect/wording-developerid.csr` | the request to upload; not secret |
| `~/.appstoreconnect/private_keys/wording-developerid.key` | the matching private key; **never leaves this machine** |

1. Open <https://developer.apple.com/account/resources/certificates/add>
2. Pick **Developer ID Application**, and **G2 Sub-CA** if asked which intermediate
3. Upload `wording-developerid.csr`
4. Download the resulting `.cer`

Then the `.p12` and the three remaining secrets can be assembled locally from that `.cer`
plus the private key above.

Using Xcode's *Settings → Accounts → Manage Certificates* instead also works, but it
generates its own key pair in the login keychain, so the `.p12` has to be exported from
Keychain Access afterwards.

Developer ID certificates are limited per team and awkward to replace, so check
`GET /v1/certificates` before creating another one.

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
