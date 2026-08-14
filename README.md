Wording App
==========
Wording helps you learn word and sentence translations between languages. It lives in
the Windows system tray and shows a word in a notification every few seconds while you
work, so learning happens in the background.

Words you mark as known come back less and less often; words you forget come back soon.
Scheduling uses the SM-2 spaced repetition algorithm, and you grade the last shown word
from the tray icon's context menu.

Your words live in `words.json` in the per-user data directory (`%APPDATA%\Wording` on
Windows), and can be edited with the application or with any text editor. A sample
English → Polish pack is committed in the repository and is imported automatically on
first run, along with any `WordsData.xml` left over from older versions.

Requires the .NET 10 Desktop Runtime on Windows.

```
dotnet build Wording.slnx
dotnet test
dotnet publish src/Wording.WordApp/Wording.WordApp.csproj -c Release -r win-x64 --self-contained false -o out
```
