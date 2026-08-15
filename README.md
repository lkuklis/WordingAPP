Wording App
==========
Wording helps you learn word and sentence translations between languages. It lives in
the system tray on Windows and the menu bar on macOS, and shows a word in a notification
every few minutes while you work, so learning happens in the background.

Words you mark as known come back less and less often; words you forget come back soon.
Scheduling uses the SM-2 spaced repetition algorithm. On macOS you grade the word
straight from the notification's action buttons; on Windows you grade it from the tray
icon menu.

## Install

Grab the latest build from the [Releases page](https://github.com/lkuklis/WordingAPP/releases).

**macOS** — download the `.dmg`, open it and drag Wording to Applications. The app has
no Dock icon; look for the book glyph in the menu bar. macOS will ask for permission to
send notifications the first time — the app is useless without it.

**Windows** — download the `-setup.exe` installer. It is not code signed, so SmartScreen
will show *"Windows protected your PC"*: click **More info → Run anyway**. If you would
rather not use an installer, the `-portable.zip` contains the same build; unzip it
anywhere and run `Wording.WordApp.exe`. Nothing else needs installing — the .NET runtime
travels with the app.

## Your words

Everything lives in one `words.json` file:

- Windows: `%APPDATA%\Wording\words.json`
- macOS: `~/Library/Application Support/Wording/words.json`

Edit it with the app or with any text editor. Review progress is stored alongside each
word, so pointing both apps at the same synced file keeps your learning in step across
machines.

Wording starts empty — no sample words are added, so every word in the file is one you
chose. The file itself is created the first time you add one. On a first run the app
sends a single welcome notification so you can tell straight away that notifications are
getting through.

Upgrading from a pre-2026 version: put your old `WordsData.xml` next to
`Wording.WordApp.exe` and it is imported once, on the next start, into `words.json`.

## Building from source

Requires the .NET 10 SDK; the macOS app additionally needs Xcode command line tools.

```bash
dotnet build Wording.slnx        # Windows app and shared logic
dotnet test                      # 125 tests

cd macos
swift test                       # 83 tests
./build-app.sh && open build/Wording.app
```

The Windows project compiles on macOS and Linux too, it simply cannot run there. See
[RELEASING.md](RELEASING.md) for how releases are built and signed.

## License

Copyright © 2013–2026 Lukasz Kuklis

Wording is free software: you can redistribute it and modify it under the terms of the
**GNU General Public License, version 3** or (at your option) any later version.

It is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY — without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See
the [LICENSE](LICENSE) file for the full text.

In practice this means anyone may use, study and improve Wording freely, but anyone who
distributes a modified version has to publish its source under the same licence. A
closed-source product cannot be built on top of it.
