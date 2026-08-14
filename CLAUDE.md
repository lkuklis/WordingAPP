# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this app is

Wording is a tray/menu-bar application for learning vocabulary. Originally written in 2013 against .NET Framework 4.6, migrated to .NET 10 in 2026. A timer fires every few seconds and shows a word in a system notification — title is the original, body is the translation — so learning happens ambiently while the user works on something else.

The user grades the last shown word (*I know it* / *Hard* / *Don't know*), which feeds an SM-2 spaced repetition schedule. The main window lists all words with their review state and allows adding, grading, and deleting.

## Layout

- `Wording.slnx` — solution in the `.slnx` XML format, the .NET 10 SDK default. Regenerate as classic `.sln` with `dotnet new sln --format sln` if an older Visual Studio can't open it.
- `src/Wording.Core/` — `net10.0`, **deliberately not `-windows`**. All logic: `Learning/` (SM-2 scheduler, weighted selector, review state), `Storage/` (JSON store, legacy XML importer, path resolution), and the `WordManager` façade. Knows nothing about configuration or UI.
- `src/Wording.Shell/` — `net10.0`. Shared by both UI shells: `WordingSettings` (reads `appsettings.json`), `WordingHost` (composition root), `WordRow` (list projection). Ships `appsettings.json`.
- `src/Wording.WordApp/` — `net10.0-windows` WinForms shell. `EnableWindowsTargeting` lets it compile on non-Windows hosts. Windows-only at runtime.
- `src/Wording.Desktop/` — `net10.0` Avalonia shell. **Runs on macOS**, which is why it exists: the WinForms shell cannot be exercised without Windows. Also runs on Windows.
- `tests/Wording.Core.Tests/` — xUnit, `net10.0`, 49 tests, runs on macOS.

Two shells is deliberate, not accidental duplication. Both are thin over `Wording.Shell` + `Wording.Core`; the WinForms one is the path to first-class Windows toasts, the Avalonia one is the path to running and iterating without Windows. Neither has been chosen as *the* final shell yet.

## Architecture

**One store per process.** `WordingHost.Create()` reads settings, opens one `JsonWordStore`, wraps it in one `WordManager`, and every screen receives that same instance. Pre-migration, each screen constructed its own repository, so the add dialog wrote through a different in-memory copy than the main window and the grid needed a manual reload to notice. `WordManagerTests.WspolnyMagazyn_ObaEkranyWidzaTeSameDaneBezOdswiezania` guards the regression.

**Selection is weighted, not gated.** `WordSelector` does *not* filter to words whose `DueUtc` has passed, which is what a conventional SRS would do. This app shows a word every few seconds rather than in review sessions, so due-date gating would leave it with nothing to display most of the time. Instead every word gets a weight — new words highest, then overdue ones scaling with lateness (capped at 30 days so one long-forgotten word can't dominate), then a small non-zero floor for well-known words so nothing leaves rotation entirely. Measured: after grading half the starter pack as known, those take ~0.2% of impressions but still appear. Tune the constants in `WordSelector` rather than adding filtering.

**`SpacedRepetitionScheduler` is a pure function** over `(ReviewState, ReviewGrade, DateTimeOffset)`; `ReviewState` is an immutable record. Time comes from an injected `TimeProvider` everywhere, so tests use `FakeTimeProvider` and never sleep.

**JSON persistence is source-generated, not reflection-based.** `WordJsonContext` carries the `[JsonSerializable]` declarations. Not a style preference: hosts that set `JsonSerializerIsReflectionEnabledByDefault=false` (file-based apps are one) throw `InvalidOperationException` at the first serialize, and trimming or NativeAOT break it the same way. **If you add a type to the persisted graph, add it to `WordJsonContext`** — the tests run in a host with reflection enabled and will *not* catch the omission.

**Saves are atomic** — write to `<path>.tmp`, then `File.Move(overwrite: true)`.

## Data

`words.json` in the per-user data directory: `%APPDATA%\Wording` on Windows, `~/Library/Application Support/Wording` on macOS (`SpecialFolder.ApplicationData` points at `~/.config` there, which is wrong for macOS, hence the explicit branch in `WordingPaths`). Override via `wording:dataFile` in `appsettings.json`.

Ids are GUIDs. The old `Id = max + 1` was recomputed on every add, so deleting the highest-numbered word freed its id for reuse — unusable as a sync key. Review state travels inside the word record so both shells, and a future native macOS port, stay in sync from one file.

On first run `JsonWordStore.OpenOrMigrate` imports any `WordsData.xml` found next to the executable or in the working directory, assigning fresh GUIDs. The shipped `WordsData.xml` starter pack (38 English→Polish entries) doubles as the seed for new installs; `StarterPackMigrationTests` exercises the real file, not a fixture.

## Build, test, run

Requires the .NET 10 SDK (`~/.dotnet` here; `DOTNET_ROOT` and PATH are set in `~/.zshrc`).

```bash
dotnet build Wording.slnx
dotnet test
dotnet test --filter FullyQualifiedName~WordSelectorTests              # one class
dotnet test --filter "FullyQualifiedName~Waga_JestOgraniczonaZGory"    # one test

dotnet run --project src/Wording.Desktop                               # runs on macOS
```

`Wording.WordApp` **compiles** on macOS but cannot run there. Verify WinForms changes by compiling, then run on Windows. There is no WinForms designer on macOS — edit `.Designer.cs` by hand, keeping it consistent with the paired `.resx`.

For pure logic work, `dotnet run some.cs` as a .NET 10 file-based app against `Wording.Core` is the fastest loop. Such hosts disable reflection-based JSON, which is a feature here — it catches serialization regressions the test project cannot.

Producing a Windows binary from macOS:

```bash
# framework-dependent — small, needs .NET 10 Desktop Runtime on the target
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 --self-contained false -o out

# self-contained single file — ~111 MB, no prerequisites (replaces the old ILMerge step)
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o out
```

`PublishTrimmed` is unsupported for WinForms, so self-contained size can't meaningfully shrink.

## Known gaps

- **Neither shell has notifications with action buttons yet.** WinForms uses `NotifyIcon.ShowBalloonTip`, which Windows 10+ reroutes to the toast system while ignoring the timeout. Avalonia has no notification API at all, so `MacNotifier` shells out to `osascript display notification`, which also has no buttons. Grading therefore lives in the tray/menu-bar menu in both. Windows App SDK toasts (needing COM activator registration for unpackaged apps) and macOS `UNUserNotificationCenter` are the respective fixes.
- **`MacNotifier` deliberately avoids a notification library.** The one Avalonia option (`DesktopNotifications`) has ~30k downloads, and notifications are the whole product here — not a place for a dependency that size.
- **The WinForms grid is read-only.** Cell editing was never persisted in the original either. Deleting rows works. The Avalonia shell has explicit add/grade/delete buttons instead.
- **The Avalonia app is not bundled as a `.app`**, so on macOS it shows a Dock icon alongside the menu-bar icon. Proper bundling is needed for it to behave like a real menu-bar-only app.
- **A native macOS port** as a SwiftUI menu-bar app sharing `words.json` remains an option if the Avalonia shell's notification limits prove too tight. Keep `Wording.Core` free of Windows-only dependencies either way.

## Conventions

- Code comments, test names, and commit messages are in Polish; user-facing UI strings are in English, matching the original application.
- `TreatWarningsAsErrors` is on in `Wording.Core` and `Wording.Shell`.
- `InternalsVisibleTo` exposes `WordSelector.Weight` and its constants to the test project; keep implementation details internal rather than widening the public API for tests.
- `AVLN3001` is suppressed in `Wording.Desktop`: windows are constructed in code with their dependencies, so the XAML runtime loader's parameterless-constructor requirement does not apply.
