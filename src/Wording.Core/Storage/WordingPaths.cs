namespace Wording.Core.Storage;

/// <summary>
/// Ustala, gdzie leza dane uzytkownika.
/// <para>
/// Stara wersja czytala "WordsData.xml" wzgledem katalogu roboczego procesu, wiec
/// uruchomienie skrotem z innego miejsca wywalalo aplikacje, a dane siedzialy
/// obok pliku exe (czyli ginely przy kazdej aktualizacji). Teraz jest to katalog
/// danych uzytkownika, wlasciwy dla systemu.
/// </para>
/// </summary>
public static class WordingPaths
{
    public const string AppFolderName = "Wording";

    public const string DataFileName = "words.json";

    /// <summary>Nazwa pliku w starym formacie XML, z ktorego robimy jednorazowy import.</summary>
    public const string LegacyDataFileName = "WordsData.xml";

    /// <summary>
    /// Katalog danych: %APPDATA%\Wording na Windows,
    /// ~/Library/Application Support/Wording na macOS.
    /// </summary>
    public static string DataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            // Na macOS SpecialFolder.ApplicationData wskazuje ~/.config, a natywne
            // miejsce na dane aplikacji to Library/Application Support.
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                AppFolderName);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);
    }

    public static string DataFile() => Path.Combine(DataDirectory(), DataFileName);
}
