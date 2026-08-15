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

## Every request covers both ports

**Unless the user names a platform, a request applies to `src/` *and* `macos/`.** "Fix the
interval picker", "add quiet hours", "the grading buttons are wrong" — all of them mean both
apps, because the user thinks in terms of *Wording*, one product that happens to have two
implementations. Shipping a fix to one port and calling the task done leaves the other half of
the product broken, and nothing in the build will complain: the two trees have no shared code
and no cross-port test, so a one-sided change compiles clean and passes every suite.

The default is symmetry, so it holds even when a change looks platform-flavoured. A macOS
notification bug usually has a .NET counterpart in the same logic; a WinForms wording fix
usually needs the same wording in SwiftUI. Analyse both, then either change both or say
plainly why one does not apply.

Only skip a port when the user scoped the request themselves ("only on Mac", "in the WinForms
grid"), or when the feature genuinely has no counterpart — the legacy XML importer is .NET-only
by design, notification action buttons have no `ShowBalloonTip` equivalent. Both are decisions
worth stating in the reply, not silent omissions.

What this means in practice:

- Change `Learning/`, `Storage/`, or the word model in .NET → make the matching change in
  `WordingKit`, and vice versa. These are line-for-line ports.
- Change persisted JSON, or the pack format → update `WordJsonContext`, the Swift `Codable`
  types, and `InteropTests`, or the two apps stop reading each other's files.
- Change a validation limit or rule → `PackLimits` and `PackSlug` exist in both ports with
  the same numbers, and a pack accepted by one must be accepted by the other.
- Add a test on one side → add the equivalent on the other, so the counts stay meaningful.

**Verification is asymmetric even when the change is not, and which half is verifiable depends
on the machine you are running on.** Only one port can actually be launched from any given
host, so the same change lands with two different levels of proof:

| Working on | Can run and verify | Compiles and unit-tests only |
|---|---|---|
| macOS | `macos/` (`./build-app.sh && open build/Wording.app`) | `src/` — WinForms needs Windows |
| Windows | `src/` (`dotnet run` the WinForms app) | `macos/` — Swift needs a Mac |

Both directions are normal and expected. Make the change in both ports either way, then say
which half you actually exercised: *"changed in both; verified by running on Windows, the Swift
side compiles and passes `swift test` but is unrun until it is opened on a Mac."* Do not let
the unverifiable half quietly become the untouched half — deferring the *code* to the other
machine is how the two ports drift apart, while deferring only the *verification* is fine.

Whichever port you could not run, note it as pending in the reply so the user knows what to
check when they next switch machines. See *Verification lessons worth keeping* — every bug in
this project's history came from treating a compile as a run.

## Layout

- `Wording.slnx` — .NET solution in the `.slnx` XML format (the .NET 10 SDK default). `dotnet new sln --format sln` regenerates a classic `.sln` if an older Visual Studio can't open it.
- `src/Wording.Core/` — `net10.0`, **deliberately not `-windows`**. All .NET logic: `Learning/` (SM-2, weighted selector), `Storage/` (JSON store, legacy XML importer, paths). Knows nothing about configuration or UI, so it builds and tests on macOS.
- `src/Wording.WordApp/` — `net10.0-windows` WinForms app, its settings, and the list projection. `Program.Main` is the composition root. `EnableWindowsTargeting` lets it compile on non-Windows hosts.
- `tests/Wording.Core.Tests/` — xUnit, 172 tests, runs on macOS.
- `macos/` — SwiftPM package. `WordingKit` (logic port) and `WordingApp` (SwiftUI). 116 tests. No target declares resources, so SwiftPM produces no resource bundle. `build-app.sh` assembles and signs `Wording.app`; `make-dmg.sh` wraps it in a disk image.
- `windows/Wording.iss` — Inno Setup script for the Windows installer. Only ever built in CI; it cannot be compiled on macOS.
- `learning_data/` — published word packs, one JSON each, validated by both test suites.
- `RELEASING.md` — how a release is cut and which Apple secrets it needs. `LICENSE` — GPL-3.0.

## Architecture

**One store per process.** `Program.Main` / `AppModel.start()` open one store and hand the same manager to every screen. Pre-migration each .NET screen constructed its own repository, so the add dialog wrote through a different in-memory copy than the main window. Both composition roots are four lines and deliberately have no wrapper type — the store is concrete (`JsonWordStore` / `WordStore`), because a one-implementation interface bought nothing and no test ever substituted it.

**Migration is triggered from the composition root, not the store constructor.** `JsonWordStore.ImportLegacyIfEmpty` refuses to touch a non-empty store, so it cannot overwrite review state. Nothing is seeded on either platform — see *Data* below.

**Selection is weighted, not gated.** `WordSelector` does *not* filter to words whose `dueUtc` has passed, which is what a conventional SRS would do. This app shows a word every few minutes rather than in review sessions, so due-date gating would leave it with nothing to display. Every word gets a weight — new words highest, overdue ones scaling with lateness (capped at 30 days so one forgotten word can't dominate), and a small non-zero floor so nothing leaves rotation. Measured: words graded known take ~0.2% of impressions but still appear. Tune the constants rather than adding filtering. Both ports implement this identically.

**The scheduler is a pure function** over `(ReviewState, ReviewGrade, Date)`, with an immutable state type, in both languages. .NET injects `TimeProvider` (tests use `FakeTimeProvider`); Swift passes `now` explicitly. No test sleeps.

**Saves are atomic** in both — write to `<path>.tmp`, then replace.

### Word sets and packs

A **pack** is content published at a URL; a **set** is a pack after import, living in its own file. They are deliberately different types. `words.json` is personal state — identifiers and review progress — so if a pack had the same shape, a published one would carry its author's review history and importing it would either overwrite the reader's progress or invent one.

```
<data dir>/words.json          the user's own words, no set header
<data dir>/sets/<slug>.json    one imported set per file, same shape plus a "set" header
```

**An import never writes to `words.json` and never to another set.** That is the whole point: a download cannot disturb what the user is learning from at the time. Re-importing merges — words already present keep their review state, because resetting someone's progress is the one thing an import must never do. Words dropped upstream are *not* deleted locally.

**Deleting every word takes a timestamped copy first**, into a `backups/` subdirectory of whatever file it belongs to. Two details are load-bearing: the stamp (clearing an already-cleared store would otherwise overwrite the useful backup with a copy of nothing), and the *subdirectory* — the set catalogue lists `*.json` directly inside `sets/` and is non-recursive in both ports, so a backup written as a sibling would appear in the UI as a set of its own. A test in each port pins that.

**One *active* store per process, and switching goes through the composition root.** `AppModel.switchTo(setId:)` / `WordingMain.SwitchTo` are the only places that replace the store, and both **clear the pending grade**: `lastShown` holds a word from the set being closed, so a grade applied after the switch would either miss or land on an unrelated word. The choice is remembered per platform — `UserDefaults` on macOS, a one-line `active-set.txt` in the data directory on Windows, since WinForms has nothing writable. `WordSetCatalog.ResolveActiveFile` falls back to the user's own words when the remembered set has been deleted or its id is not a safe slug; refusing to start because a remembered set is gone would leave the user with an app that will not open.

**`PackLimits` is duplicated across the ports on purpose, and each suite pins the numbers as literals** rather than reading them from the constant. Changing one port then fails the other's test with a message naming the file to change. The two apps import the same published packs, so limits that disagree mean a pack one accepts and the other refuses. `learning_data/PROMPT.md` quotes those same numbers for contributors generating a pack with an AI, and a test fails if the prompt stops mentioning any of them — a prompt drifted from the validator produces files that look right and are refused on import, which is worse than having no prompt.

**The catalogue is the one registry in this app, and only because a remote directory cannot be listed.** `learning_data/index.json` lets the import window show names, descriptions and counts without downloading every pack — the GitHub contents API would return file names only, at one request per pack. An entry carries **no address and no file name**: `PackSource.PackUrl` derives it from the entry's `id` and the address the index itself came from, so a catalogue fetched off the internet cannot point the app somewhere else, and the same file works unchanged in a fork. A malformed row is dropped rather than fatal, because the catalogue is the only way most people will ever find a pack. `build-index.sh` regenerates it and a test in each port compares it against the files on disk.

`PackSource.OfficialIndexUrl` points at a **branch, not a tag**: a pack added to the repository appears in already-installed versions, so the catalogue grows without a release. The other half of that bargain is that a broken index breaks the browse window for every version at once — which is why CI validating `learning_data/` matters more than it looks.

**`learning_data/` publishes packs, and both test suites validate every file in it** with the same parser the app uses, locating the directory from the test's own source path (`[CallerFilePath]` / `#filePath`) rather than the working directory. Contributions come from strangers, so a pack that would be refused on import has to fail the build instead. Each suite also asserts the directory was found — an empty theory would otherwise pass while checking nothing.

**The installed sets are the directory listing, not a registry file.** A registry has to be kept in step with the disk and stops matching it the moment a file is moved by hand. For the same reason word counts are read from each file rather than stored: a stored count starts lying as soon as a word is deleted.

**`PackSlug` is a security boundary, not tidiness.** The pack's `id` decides which file gets written, and it arrives inside a file fetched from an arbitrary URL — an id of `../words.json` would overwrite exactly the data this design protects. It is an allow-list (`[a-z0-9-]`, bounded length, no leading or trailing hyphen, Windows reserved names refused *on both platforms*), and an id that fails is **refused, never cleaned up**: silently rewriting one would let two different packs collapse onto the same file.

`WordPackReader` refuses structural problems but *truncates* the two display-only fields, name and description. The user did not write the file and cannot fix it, so failing a whole pack over a long title would leave them with no way forward. Control characters are folded to spaces — they would otherwise reach a notification body.

`PackDownloader` accepts https only (re-checked after redirects), caps the payload while reading rather than after, and takes its transport by injection. Every rule there fires only on input nobody sends by accident, which is exactly the kind that goes untested when reaching it needs a real server — so the tests stub the transport and none of them touch the network.

**Adding to the persisted graph still means updating `WordJsonContext`.** `WordPack` is in it. Verify with `dotnet run` on a file-based app, which disables reflection-based JSON: `scratchpad/packcheck.cs` in the session directory is the shape of that check.

### .NET-specific

**JSON persistence is source-generated, not reflection-based.** `WordJsonContext` carries the `[JsonSerializable]` declarations. Not a style preference: hosts that set `JsonSerializerIsReflectionEnabledByDefault=false` (file-based apps are one) throw at the first serialize, and trimming or NativeAOT break it the same way. **Adding a type to the persisted graph means adding it to `WordJsonContext`** — the tests run with reflection enabled and will *not* catch the omission.

### Swift-specific

**The package must build on the CI runner's Xcode, not just the newest one.** The
`macos-15` runner ships Xcode 16.4 / SDK 15.5, where `UserNotifications` has no
concurrency annotations — `UNNotificationSettings` is not `Sendable`, so returning it
across an isolation boundary is a hard error under Swift 6. Newer SDKs annotate it and
compile either way, which is exactly why this passed locally and failed in CI. Both files
that touch the framework use `@preconcurrency import UserNotifications`. Do not "fix" that
by requiring a newer Xcode; anyone building from source would hit the same wall.

**The app must run from `Wording.app`, not `swift run`.** `UNUserNotificationCenter.current()` traps in a bare executable because there is no bundle identifier. `macos/build-app.sh` assembles the bundle and writes `Info.plist` (`com.lkuklis.wording`, `LSUIElement` so there is no Dock icon). It takes `VERSION`, `UNIVERSAL=1` and `SIGN_IDENTITY` from the environment: with no identity it ad-hoc signs, which is fine on the machine that built it but is exactly what Gatekeeper rejects once a file has been downloaded. CI passes a real Developer ID and adds the hardened runtime.

**Three serialization traps the interop tests exist to catch**, all of which would silently corrupt a data file carried over from the other platform:
- Swift encodes `UUID` uppercase, System.Text.Json writes lowercase. Without the manual `encode`, the first macOS save rewrites every id in the file. (Only the encoder is hand-written; the synthesized decoder is fine because `UUID(uuidString:)` is case-insensitive.)
- .NET writes six fractional-second digits (`22:18:18.405614+00:00`); Swift's `.iso8601` strategy rejects fractional seconds outright, so `WordingJSON` uses a custom strategy with a non-fractional fallback.
- **`Date.ISO8601FormatStyle` cannot replace `ISO8601DateFormatter` here**, however tempting its `Sendable` conformance is. It *truncates* the fraction to milliseconds instead of rounding, and combined with binary floating point the round trip does not converge: `.405614` → `.405` → `.404` → `.404`. Every save would walk timestamps backwards. `ISO8601DateFormatter` rounds correctly. `InteropTests.aFullRoundTripLosesNothing` catches this.

`WordingJSON` therefore builds a fresh `ISO8601DateFormatter` inside each coding closure rather than sharing one: the strategies are `@Sendable` and the formatter is not. That costs roughly 14 ms per whole-file save, which is imperceptible at this size and avoids a `nonisolated(unsafe)` escape. The package builds in Swift 6 language mode with no concurrency escapes at all.

## Data

`words.json` in the per-user data directory: `%APPDATA%\Wording` on Windows, `~/Library/Application Support/Wording` on macOS. (`SpecialFolder.ApplicationData` resolves to `~/.config` on macOS, which is wrong there, hence the explicit branch in the .NET `WordingPaths`.) Override the .NET path via `wording:dataFile` in `appsettings.json`.

Ids are GUIDs. The pre-2026 format recomputed `Id = max + 1` on every add, so deleting the highest-numbered word freed its id for reuse — a real bug regardless of how many machines are involved. Review state travels inside each word record, so copying the file to another machine carries the learning progress with it.

**Neither app seeds any words.** Both start empty and create the file on the first save; every word in it is one the user chose. A 38-word English→Polish starter pack used to be seeded on both platforms and was removed in 2026 for two reasons:

- `WordSelector` has a non-zero weight floor, so **nothing ever leaves rotation**. Words the user did not choose would keep surfacing forever, permanently diluting their own material — a sample pack is not free, it is a standing tax.
- On .NET the starter pack and the migration path were the same file. `WordsData.xml` shipped next to the executable and `WordingSettings.FindLegacyXml` probes `AppContext.BaseDirectory` first, so **our copy shadowed the user's own** — anyone migrating from a pre-2026 install got our words instead of theirs. Deleting the shipped copy is what makes that import work.

`LegacyXmlImporter` and `JsonWordStore.ImportLegacyIfEmpty` therefore stay, purely as the migration route: a pre-2026 user drops their `WordsData.xml` next to the executable and it is imported once. The Swift port has no XML parser and no equivalent — a macOS user has no pre-2026 install to migrate from.

**An empty store must not look like a broken app.** With no words the timer fires and shows nothing, so a new user would see a tray icon and then silence, which is indistinguishable from notification permission having been refused. Both ports therefore send **one welcome notification at startup when the store is empty** (`NotificationService.showWelcome` / a `ShowBalloonTip` in the `WordingMain` constructor), and both show an empty state in the list window instead of a blank table. There is no persisted "already welcomed" flag: the store being empty is the condition, so it stops on its own once a word is added.

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

The commands above assume a Mac. `Wording.WordApp` **compiles** on macOS but cannot run there: verify WinForms changes by compiling, then run them on Windows. There is no WinForms designer on macOS — edit `.Designer.cs` by hand, consistent with the paired `.resx`.

On Windows the restriction runs the other way: `dotnet build` and `dotnet test` cover the whole .NET solution and the app itself runs, but `macos/` cannot be built at all — SwiftPM and the `UserNotifications` framework need a Mac, so Swift changes made there stay unbuilt until someone opens them on one. Neither host can verify the whole product; CI's macOS runner is the closest thing, and it still only *compiles* the WinForms side.

For pure .NET logic work, `dotnet run some.cs` as a file-based app against `Wording.Core` is the fastest loop, and such hosts disable reflection-based JSON, which usefully catches serialization regressions the test project cannot.

Producing a Windows binary from macOS:

```bash
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 --self-contained false -o out
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o out   # ~111 MB, no prerequisites
```

`PublishTrimmed` is unsupported for WinForms.

## Verification lessons worth keeping

Every bug that survived into this project's history was a case of a green signal being
mistaken for a working feature. Compiling is not running; exiting 0 is not succeeding;
a passing test suite covers only the host it ran on.

- `osascript display notification` **exits 0 even when nothing is displayed.** It posts as Script Editor and is dropped silently without that app's permission. Exit codes prove nothing about notification delivery.
- A notification being **delivered to Notification Center is not the same as a banner being displayed.** `terminal-notifier -list` proves the former only. Alert style and Focus decide the latter, and both are TCC-protected — unreadable from a shell, so the user has to check System Settings.
- **The app bundle was missing its resource bundle for weeks** and nobody noticed, because the failure only happens with no `words.json` on disk — every new user, no developer. The resource is gone now, but the habit is the point: test the first-run path deliberately by moving the data file aside and launching. Quit any installed copy first, or it rewrites the file from memory and you end up testing nothing.
- **The Swift code silently required the newest Xcode.** It compiled locally against SDK 26 and failed on CI's Xcode 16.4, where `UserNotifications` lacks concurrency annotations. A local build proves one toolchain, not the range.
- **Git Bash rewrote `/DAppVersion=…` into a Windows path**, so the installer compiler saw two script names. Anything Windows-shaped is unverifiable from macOS; expect the first CI run on it to fail.

## Release pipeline

`.github/workflows/ci.yml` builds and tests both stacks on pushes to `master` and on
pull requests, using a macOS runner — the only one that covers Swift *and* (thanks to
`EnableWindowsTargeting`) the WinForms project.

`.github/workflows/release.yml` fires on a `v*` tag and produces the Windows installer,
a portable zip, and a notarised universal `.dmg`. Versions are CalVer (`YYYY.M.PATCH`)
and the **git tag is the only source of the version number** — `build-app.sh` writes it
into `Info.plist` and the workflow passes it to `dotnet publish` as `-p:Version=`. Never
hardcode a version in a file.

All six signing secrets are already configured and the Developer ID certificate is valid
to 2031-08-16, so a routine release is just a tag. `RELEASING.md` covers the setup for
when that changes. To rehearse without publishing, run the workflow manually — a
`workflow_dispatch` run builds artefacts but creates no release, because publishing is
gated on `github.event_name == 'push'`.

Verified on 2026.8.0 and again on 2026.8.1: downloading the published `.dmg`, setting the
quarantine attribute and mounting it, `spctl -a -vvv` on **`Wording.app` inside** reports
`accepted / source=Notarized Developer ID`.

Assess the app, not the disk image. `spctl` on the `.dmg` itself answers
`rejected / source=no usable signature`, and `codesign -dv` calls it unsigned — for both
releases, so it is how this pipeline has always worked, not a regression. The image is
notarised and stapled (`xcrun stapler validate` on the `.dmg` passes) but never
code-signed, and Gatekeeper gates the launch of the app. Checking the wrong object here
costs an afternoon chasing a signing bug that does not exist.
Creating the Developer ID certificate is the one step that cannot be automated — the App
Store Connect API returns 403, Account Holder only.

**If a SwiftPM resource is ever added back, `macos/build-app.sh` must copy its bundle into
`Contents/Resources`.** This used to be live: `Bundle.module` calls `fatalError` when its
bundle is absent, so the starter pack loader killed the app at launch — but only on a
machine with no `words.json` yet, which is every new user and no developer. Dropping the
starter pack removed the last resource, and with it the copy step and the CI assertion
that guarded it. CI now checks the executable and `CFBundleIdentifier` instead.

## Known gaps

- **The Windows app has never been run**, only compiled. Its tray menu, grading, and grid are unverified.
- **Windows notifications are still `ShowBalloonTip`**, which Windows 10+ reroutes to the toast system while ignoring the timeout, and which has no action buttons. Windows App SDK toasts (needing COM activator registration for unpackaged apps) are the equivalent of what the Swift app already does.
- **The Swift app has no quiet hours**, only a pause and an interval picker (5 s – 1 h, default 30 s). The .NET app has neither and reads its interval from `appsettings.json`.
- **The Windows installer is unsigned**, so SmartScreen warns until a code-signing certificate is bought. The macOS side is signed and notarised.

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
- The two ports are meant to read alike line for line, so keep names and structure symmetric across them. A change to one is a change to both unless the user scoped it otherwise — see *Every request covers both ports*.
- `TreatWarningsAsErrors` is on in `Wording.Core`. Both stacks currently build with zero warnings — keep it that way.
- `InternalsVisibleTo` exposes `WordSelector.Weight` and its constants to the .NET test project; keep implementation details internal rather than widening the public API for tests.
