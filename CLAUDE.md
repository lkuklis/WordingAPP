# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this app is

Wording is a Windows desktop vocabulary-drilling app (originally written 2013, migrated from .NET Framework 4.6 to .NET 10 in 2026). It sits in the system tray and, on a timer, pops a Windows notification showing a random word from the user's list: the notification **title** is the original word, the **body** is its translation. Learning is meant to happen through passive repetition — you keep seeing words while doing other work.

The main window is a `DataGridView` of all word pairs plus an **Add** button; minimizing hides the window to the tray, clicking the tray icon restores it. Deleting a grid row deletes the word. The shipped sample pack is English → Polish.

Translations live in a plain XML file (`WordsData.xml`, `<AllWords>/<Word>/<Id|Original|Translated>`) which can be edited by the app or by hand.

## Layout

- `Wording.slnx` — solution in the new XML format (`.slnx`), the .NET 10 SDK default. Regenerate as classic `.sln` with `dotnet new sln --format sln` if an older Visual Studio can't open it.
- `src/Wording.Core/` — `net10.0` class library, **deliberately not `-windows`**: it holds all logic and must stay platform-independent so it builds, runs, and tests on macOS. Domain (`Word`), XML-backed store (`Repository/WordRepository.cs` implementing `IRepository`), the `WordManager` façade, and `RandomValue.GetRandom<T>()`. Ships `WordsData.xml` as content, copied to output on build.
- `src/Wording.WordApp/` — `net10.0-windows` WinForms executable. `WordingMain` owns the `NotifyIcon`, the `Timer`, and the grid; `NewWord` is the add-word dialog. Sets `EnableWindowsTargeting` so it compiles on non-Windows hosts.
- `tests/Wording.Core.Tests/` — xUnit, `net10.0`. Runs natively on macOS.

## Architecture notes that aren't obvious from one file

- **No database despite appearances.** The whole store is `XDocument` in memory, re-saved to disk after every add/delete/edit. `WordRepository` loads its file eagerly in the constructor and caches the list; `RefreshData()` is the only way to reload. `WordManager.GetWords()` returns the cache, `GetWordsData()` forces a refresh first — the UI uses the latter.
- **Every consumer constructs its own repository.** `WordManager` news up `WordRepository` directly, and `WordingMain` and `NewWord` each new up their own `WordManager`. So the add dialog writes through a *different* in-memory copy than the main form, which is why the main form has to re-read the file after `DialogResult.OK`. There is no DI container and `IRepository` is never substituted. `WordRepositoryTests.GetAll_NieWidziZmianBezRefreshData` pins this behaviour.
- **The data file path is a bare filename** (`defaultFileName` in `App.config`, value `WordsData.xml`) resolved relative to the current working directory — not to the assembly location and not to a per-user AppData path. Launching from a different cwd breaks the app.
- **Config keys in `App.config`:** `changeTime` (seconds between notifications) and `showTime` (intended balloon duration). Note `WordingMain` computes *both* `_showTime` and `_changeTime` from the `changeTime` key — `showTime` is read from config nowhere. Preserve or deliberately fix this when refactoring; don't "fix" it silently.
- **`Id` is `max(Id) + 1`, recomputed per add**, so deleting the highest-numbered word frees its id for reuse, and ids are not contiguous (id 6 is missing in the sample data). Pinned by `AddWord_PoUsunieciuNajwyzszegoId_UzywaIdPonownie`. This is why any cross-device sync needs GUIDs instead.
- **`EditWord` throws on an unknown id** — `FirstOrDefault` returns null and is dereferenced immediately.
- **There is no repetition algorithm.** `RandomValue.GetRandom` picks uniformly at random; nothing tracks what the user knows. Despite the app's stated purpose, spaced repetition is unimplemented.
- **`NotifyIcon.ShowBalloonTip` is legacy.** Windows 10+ routes balloon tips to the toast system and ignores the timeout argument, so the `showTime` bug above is doubly inert. Modern toasts with action buttons need the Windows App SDK (`Microsoft.WindowsAppSDK`) plus COM activator registration for unpackaged apps.

## Build and test

Requires the .NET 10 SDK (`~/.dotnet` on this machine, `DOTNET_ROOT` and PATH set in `~/.zshrc`).

```bash
dotnet build Wording.slnx           # builds all three projects
dotnet test                         # Core tests — these run on macOS
dotnet test --filter FullyQualifiedName~WordRepositoryTests   # one test class
dotnet test --filter "FullyQualifiedName~AddWord_ZapisujeDoPliku"  # one test
```

`Wording.WordApp` **compiles** on macOS thanks to `EnableWindowsTargeting`, but cannot run there — WinForms needs Windows. Verify UI changes by compiling locally, then run on Windows for anything involving the tray icon or notifications. There is no WinForms designer on macOS; edit `.Designer.cs` by hand and keep it consistent with the paired `.resx`.

Producing a Windows binary from macOS:

```bash
# framework-dependent — ~200 KB, needs .NET 10 Desktop Runtime on the target machine
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 --self-contained false -o out

# self-contained single file — ~111 MB, no prerequisites (replaces the old ILMerge step)
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -o out
```

`PublishTrimmed` is not supported for WinForms, so the self-contained size cannot be meaningfully reduced.

## Working in this repo

- Keep `Wording.Core` free of any Windows-only dependency. It is the shared contract for a planned native macOS port, and its testability on macOS depends on it.
- `WordRepository` has a `(string documentFileName)` constructor for tests; the parameterless one reads `App.config` and preserves the original behaviour. Prefer the explicit one in new code.
- Sample data in `WordsData.xml` is committed intentionally (English→Polish starter pack) — treat it as a fixture, not as user state.
- Test names are in Polish, matching the working language of the project's issues and commits.
