# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this app is

Wording is a tray/menu-bar application for learning vocabulary. Originally written in 2013 against .NET Framework 4.6, migrated to .NET 10 in 2026 and given a native macOS port.

A timer fires periodically and shows a word in a system notification — title is the original, body is the translation — so learning happens ambiently while the user works on something else. The user grades the word (*I know it* / *Hard* / *Don't know*), which feeds an SM-2 spaced repetition schedule.

## Two apps, one data format

| | `src/` (.NET) | `macos/` (Swift) |
|---|---|---|
| Platform | Windows | macOS |
| UI | WinForms | SwiftUI `MenuBarExtra` |
| Notifications | `NotifyIcon.ShowBalloonTip` | `UNUserNotificationCenter` |
| Grading | tray context menu | **action buttons in the notification** |
| Status | compiles clean, **never run** | verified working end to end |

This is deliberate. Notification APIs are the least abstractable part of the platform and they *are* the product here, so each platform uses its own native one and the two apps share only the data format, not code. An Avalonia shell existed briefly as scaffolding to run something on macOS before the native port; it was removed once the Swift app worked.

**The two apps write the same `words.json` shape, but they are not expected to share a live file.** A Windows user runs the Windows app, a macOS user runs the macOS one. Keeping the formats identical is insurance for the case that does occur — someone moving machines, or keeping their data directory in a synced folder — and it is cheap now that `macos/Tests/WordingKitTests/InteropTests.swift` pins it. Treat it as a compatibility guarantee worth keeping, not as a requirement that should drive design decisions.

## Layout

- `Wording.slnx` — .NET solution in the `.slnx` XML format (the .NET 10 SDK default). `dotnet new sln --format sln` regenerates a classic `.sln` if an older Visual Studio can't open it.
- `src/Wording.Core/` — `net10.0`, **deliberately not `-windows`**. All .NET logic: `Learning/` (SM-2, weighted selector), `Storage/` (JSON store, legacy XML importer, paths). Knows nothing about configuration or UI, so it builds and tests on macOS.
- `src/Wording.WordApp/` — `net10.0-windows` WinForms app, its settings, and the list projection. `Program.Main` is the composition root. `EnableWindowsTargeting` lets it compile on non-Windows hosts.
- `tests/Wording.Core.Tests/` — xUnit, 49 tests, runs on macOS.
- `macos/` — SwiftPM package. `WordingKit` (logic port + starter pack) and `WordingApp` (SwiftUI). 43 tests.

## Architecture

**One store per process.** `Program.Main` / `AppModel.start()` open one store and hand the same manager to every screen. Pre-migration each .NET screen constructed its own repository, so the add dialog wrote through a different in-memory copy than the main window. Both composition roots are four lines and deliberately have no wrapper type — the store is concrete (`JsonWordStore` / `WordStore`), because a one-implementation interface bought nothing and no test ever substituted it.

**Seeding and migration are triggered from the composition root, not the store constructor.** `JsonWordStore.ImportLegacyIfEmpty` and `WordStore.seedIfEmpty` both refuse to touch a non-empty store, so neither can overwrite review state written by the other app.

**Selection is weighted, not gated.** `WordSelector` does *not* filter to words whose `dueUtc` has passed, which is what a conventional SRS would do. This app shows a word every few minutes rather than in review sessions, so due-date gating would leave it with nothing to display. Every word gets a weight — new words highest, overdue ones scaling with lateness (capped at 30 days so one forgotten word can't dominate), and a small non-zero floor so nothing leaves rotation. Measured: words graded known take ~0.2% of impressions but still appear. Tune the constants rather than adding filtering. Both ports implement this identically.

**The scheduler is a pure function** over `(ReviewState, ReviewGrade, Date)`, with an immutable state type, in both languages. .NET injects `TimeProvider` (tests use `FakeTimeProvider`); Swift passes `now` explicitly. No test sleeps.

**Saves are atomic** in both — write to `<path>.tmp`, then replace.

### .NET-specific

**JSON persistence is source-generated, not reflection-based.** `WordJsonContext` carries the `[JsonSerializable]` declarations. Not a style preference: hosts that set `JsonSerializerIsReflectionEnabledByDefault=false` (file-based apps are one) throw at the first serialize, and trimming or NativeAOT break it the same way. **Adding a type to the persisted graph means adding it to `WordJsonContext`** — the tests run with reflection enabled and will *not* catch the omission.

### Swift-specific

**The app must run from `Wording.app`, not `swift run`.** `UNUserNotificationCenter.current()` traps in a bare executable because there is no bundle identifier. `macos/build-app.sh` assembles the bundle, writes `Info.plist` (`com.lkuklis.wording`, `LSUIElement` so there is no Dock icon), and ad-hoc signs it — the signature is what keeps notification permission stable across launches.

**Three serialization traps the interop tests exist to catch**, all of which would silently corrupt a data file carried over from the other platform:
- Swift encodes `UUID` uppercase, System.Text.Json writes lowercase. Without the manual `encode`, the first macOS save rewrites every id in the file. (Only the encoder is hand-written; the synthesized decoder is fine because `UUID(uuidString:)` is case-insensitive.)
- .NET writes six fractional-second digits (`22:18:18.405614+00:00`); Swift's `.iso8601` strategy rejects fractional seconds outright, so `WordingJSON` uses a custom strategy with a non-fractional fallback.
- **`Date.ISO8601FormatStyle` cannot replace `ISO8601DateFormatter` here**, however tempting its `Sendable` conformance is. It *truncates* the fraction to milliseconds instead of rounding, and combined with binary floating point the round trip does not converge: `.405614` → `.405` → `.404` → `.404`. Every save would walk timestamps backwards. `ISO8601DateFormatter` rounds correctly. `InteropTests.aFullRoundTripLosesNothing` catches this.

`WordingJSON` therefore builds a fresh `ISO8601DateFormatter` inside each coding closure rather than sharing one: the strategies are `@Sendable` and the formatter is not. That costs roughly 14 ms per whole-file save, which is imperceptible at this size and avoids a `nonisolated(unsafe)` escape. The package builds in Swift 6 language mode with no concurrency escapes at all.

## Data

`words.json` in the per-user data directory: `%APPDATA%\Wording` on Windows, `~/Library/Application Support/Wording` on macOS. (`SpecialFolder.ApplicationData` resolves to `~/.config` on macOS, which is wrong there, hence the explicit branch in the .NET `WordingPaths`.) Override the .NET path via `wording:dataFile` in `appsettings.json`.

Ids are GUIDs. The pre-2026 format recomputed `Id = max + 1` on every add, so deleting the highest-numbered word freed its id for reuse — a real bug regardless of how many machines are involved. Review state travels inside each word record, so copying the file to another machine carries the learning progress with it.

Seeding the same 38-word English→Polish starter pack happens differently per platform, and the word list is consequently duplicated:
- .NET: `JsonWordStore.ImportLegacyIfEmpty`, called from `Program.Main`, imports `WordsData.xml` found next to the executable or in the working directory. It doubles as the migration path for pre-2026 installs.
- Swift: `StarterPack` reads the bundled `starter-pack.json` and `seedIfEmpty` applies it **only to an empty store**, so it never touches a file written by the .NET app. There is no XML parser in the Swift port by design.

If the starter word list changes, change both `src/Wording.Core/WordsData.xml` and `macos/Sources/WordingKit/Resources/starter-pack.json`.

## Build, test, run

.NET requires the .NET 10 SDK (`~/.dotnet` here; `DOTNET_ROOT` and PATH set in `~/.zshrc`). Swift requires Xcode command line tools.

```bash
dotnet build Wording.slnx
dotnet test
dotnet test --filter FullyQualifiedName~WordSelectorTests

cd macos
swift test
swift test --filter InteropTests
./build-app.sh && open build/Wording.app     # the only correct way to run the macOS app
```

`Wording.WordApp` **compiles** on macOS but cannot run there. Verify WinForms changes by compiling, then run on Windows. There is no WinForms designer on macOS — edit `.Designer.cs` by hand, consistent with the paired `.resx`.

For pure .NET logic work, `dotnet run some.cs` as a file-based app against `Wording.Core` is the fastest loop, and such hosts disable reflection-based JSON, which usefully catches serialization regressions the test project cannot.

Producing a Windows binary from macOS:

```bash
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 --self-contained false -o out
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o out   # ~111 MB, no prerequisites
```

`PublishTrimmed` is unsupported for WinForms.

## Verification lessons worth keeping

Two mistakes were made and cost real time; both were "the process started" being mistaken for "the feature works".

- `osascript display notification` **exits 0 even when nothing is displayed.** It posts as Script Editor and is dropped silently without that app's permission. Exit codes prove nothing about notification delivery.
- A notification being **delivered to Notification Center is not the same as a banner being displayed.** `terminal-notifier -list` proves the former only. Alert style and Focus decide the latter, and both are TCC-protected — unreadable from a shell, so the user has to check System Settings.

## Release pipeline

`.github/workflows/ci.yml` builds and tests both stacks on every push, on a macOS runner
— it is the only runner that covers Swift *and* (thanks to `EnableWindowsTargeting`) the
WinForms project.

`.github/workflows/release.yml` fires on a `v*` tag and produces the Windows installer,
a portable zip, and a notarised universal `.dmg`. Versions are CalVer (`YYYY.M.PATCH`)
and the **git tag is the only source of the version number** — `build-app.sh` writes it
into `Info.plist` and the workflow passes it to `dotnet publish` as `-p:Version=`. Never
hardcode a version in a file. See [RELEASING.md](RELEASING.md) for the required Apple
secrets.

**`macos/build-app.sh` must copy the SwiftPM resource bundle into `Contents/Resources`.**
It is not optional packaging polish: `Bundle.module` calls `fatalError` when the bundle
is absent, so `StarterPack.load()` would kill the app at launch — but only on a machine
with no `words.json` yet, which is every new user and no developer. CI asserts the
bundle is present for exactly this reason.

## Known gaps

- **The Windows app has never been run**, only compiled. Its tray menu, grading, and grid are unverified.
- **Windows notifications are still `ShowBalloonTip`**, which Windows 10+ reroutes to the toast system while ignoring the timeout, and which has no action buttons. Windows App SDK toasts (needing COM activator registration for unpackaged apps) are the equivalent of what the Swift app already does.
- **The Swift app has no quiet hours**, only a pause and an interval picker (5 s – 1 h, default 30 s). The .NET app has neither and reads its interval from `appsettings.json`.
- **`Wording.app` is ad-hoc signed**, so it is fine locally but not distributable without a Developer ID.

## License

GPL-3.0. The full text is in `LICENSE`; the copyright notice and a plain-language summary
are in `README.md`. This is a deliberate choice over MIT/Apache: it lets anyone use and
improve Wording, but a redistributed derivative has to ship its source under the same
terms, so nobody can build a closed commercial product on it.

Two consequences worth remembering:
- **The Mac App Store is off the table.** Its terms conflict with GPL-3.0 (this is what
  got VLC pulled). Direct distribution with a Developer ID is the path, which suits this
  app anyway — sandboxing would move `words.json` into a container.
- New source files do not carry per-file GPL headers. That is a conscious trade for
  readability; `LICENSE` plus the README notice carry the licence.

## Conventions

- **Everything in the repository is English** — identifiers, comments, doc comments, test method names, UI strings, and commit messages. This is a public project; Polish appears only as *data* (the English→Polish starter pack) and in the handful of test assertions that use those words to prove non-ASCII survives a JSON round trip.
- Keep user-facing wording out of `Wording.Core`/`WordingKit`: they raise typed errors and the UI decides how to phrase them.
- The two ports are meant to read alike line for line, so keep names and structure symmetric across them.
- `TreatWarningsAsErrors` is on in `Wording.Core`. Both stacks currently build with zero warnings — keep it that way.
- `InternalsVisibleTo` exposes `WordSelector.Weight` and its constants to the .NET test project; keep implementation details internal rather than widening the public API for tests.
