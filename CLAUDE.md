# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this app is

Wording is a Windows tray application for learning vocabulary. Originally written in 2013 against .NET Framework 4.6, migrated to .NET 10 in 2026. A timer fires every few seconds and shows a word in a notification — title is the original, body is the translation — so learning happens ambiently while the user works on something else.

The user grades the last shown word from the tray icon's context menu (*I know it* / *Hard* / *Don't know*), which feeds an SM-2 spaced repetition schedule. The main window is a read-only grid of all words with their review state, plus an **Add** button. Minimizing hides to the tray.

## Layout

- `Wording.slnx` — solution in the `.slnx` XML format, the .NET 10 SDK default. Regenerate as classic `.sln` with `dotnet new sln --format sln` if an older Visual Studio can't open it.
- `src/Wording.Core/` — `net10.0`, **deliberately not `-windows`**. All logic lives here: `Learning/` (SM-2 scheduler, weighted selector, review state), `Storage/` (JSON store, legacy XML importer, path resolution), and the `WordManager` façade. Builds, runs, and tests natively on macOS.
- `src/Wording.WordApp/` — `net10.0-windows` WinForms shell. `EnableWindowsTargeting` lets it compile on non-Windows hosts.
- `tests/Wording.Core.Tests/` — xUnit, `net10.0`, 49 tests, runs on macOS.

## Architecture

**Core knows nothing about configuration or the UI.** `Program.cs` is the composition root: it reads `appsettings.json`, opens one `JsonWordStore`, wraps it in one `WordManager`, and hands that same instance to every form. This is deliberate — the pre-migration code had each screen construct its own repository, so the add dialog wrote through a different in-memory copy than the main window and the grid needed a manual reload to notice. `WordManagerTests.WspolnyMagazyn_ObaEkranyWidzaTeSameDaneBezOdswiezania` guards against a regression.

**Selection is weighted, not gated.** `WordSelector` does *not* filter to words whose `DueUtc` has passed, which is what a conventional SRS would do. This app shows a word every few seconds rather than in review sessions, so due-date gating would leave it with nothing to display most of the time. Instead every word gets a weight — new words highest, then overdue ones scaling with lateness (capped at 30 days so one long-forgotten word can't dominate), then a small non-zero floor for well-known words so nothing ever leaves rotation entirely. Measured behaviour: after grading half the starter pack as known, those words take ~0.2% of impressions but still appear. Change the constants in `WordSelector` rather than adding filtering.

**`SpacedRepetitionScheduler` is a pure function** over `(ReviewState, ReviewGrade, DateTimeOffset)`. `ReviewState` is an immutable record. Time comes from an injected `TimeProvider` everywhere, so tests use `FakeTimeProvider` and never sleep.

**JSON persistence is source-generated, not reflection-based.** `WordJsonContext` carries the `[JsonSerializable]` declarations. This is not a style preference: hosts that set `JsonSerializerIsReflectionEnabledByDefault=false` (file-based apps are one) throw `InvalidOperationException` at the first serialize, and trimming or NativeAOT would break it the same way. **If you add a type to the persisted graph, add it to `WordJsonContext`** — the unit tests run in a host with reflection enabled and will *not* catch the omission.

**Saves are atomic** — write to `<path>.tmp`, then `File.Move(overwrite: true)`.

## Data

`words.json` in the per-user data directory: `%APPDATA%\Wording` on Windows, `~/Library/Application Support/Wording` on macOS (`SpecialFolder.ApplicationData` points at `~/.config` there, which is wrong for macOS, hence the explicit branch in `WordingPaths`). Overridable via `wording:dataFile` in `appsettings.json`.

Ids are GUIDs. The old format's `Id = max + 1` was recomputed on every add, so deleting the highest-numbered word freed its id for reuse — unusable as a sync key. Review state travels inside the word record so a future macOS port reading the same file stays in sync.

On first run `JsonWordStore.OpenOrMigrate` imports any `WordsData.xml` found next to the executable or in the working directory, assigning fresh GUIDs. The shipped `WordsData.xml` starter pack (38 English→Polish entries) doubles as the seed for new installs; `StarterPackMigrationTests` exercises the real file, not a fixture.

## Build and test

Requires the .NET 10 SDK (`~/.dotnet` here; `DOTNET_ROOT` and PATH are set in `~/.zshrc`).

```bash
dotnet build Wording.slnx
dotnet test
dotnet test --filter FullyQualifiedName~WordSelectorTests              # one class
dotnet test --filter "FullyQualifiedName~Waga_JestOgraniczonaZGory"    # one test
```

`Wording.WordApp` **compiles** on macOS but cannot run there. Verify UI changes by compiling locally, then run on Windows for anything touching the tray icon or notifications. There is no WinForms designer on macOS — edit `.Designer.cs` by hand, keeping it consistent with the paired `.resx`.

For logic changes, `dotnet run some.cs` as a .NET 10 file-based app against `Wording.Core` is a fast way to see real behaviour without Windows. Note such hosts disable reflection-based JSON, which is a feature here — it catches serialization regressions the test project cannot.

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

- **Notifications are still `NotifyIcon.ShowBalloonTip`**, which Windows 10+ reroutes to the toast system while ignoring the timeout argument. Grading therefore lives in the tray context menu. Replacing this with Windows App SDK toasts carrying action buttons is the next planned step and needs COM activator registration for unpackaged apps.
- **The grid is read-only.** Cell editing was never persisted in the original either; rather than keep pretending, editing is disabled. Deleting rows works. Re-enabling edits means mapping `WordRow` back through `IWordStore.Update`.
- **A native macOS port is planned** as a separate SwiftUI menu-bar app sharing `words.json`, rather than a cross-platform UI framework — notification APIs are the least abstractable part and are the whole product here. Keep `Wording.Core` free of Windows-only dependencies.

## Conventions

- Code comments, test names, and commit messages are in Polish; user-facing UI strings are in English, matching the original application.
- `TreatWarningsAsErrors` is on in `Wording.Core`.
- `InternalsVisibleTo` exposes `WordSelector.Weight` and its constants to the test project; keep implementation details internal rather than widening the public API for tests.
