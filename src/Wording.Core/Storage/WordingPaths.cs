namespace Wording.Core.Storage;

/// <summary>
/// Decides where the user's data lives.
/// <para>
/// The pre-2026 version read "WordsData.xml" relative to the process working directory,
/// so launching from anywhere else crashed the app and the data sat next to the
/// executable (and was lost on every update). It is now the platform's per-user data
/// directory.
/// </para>
/// </summary>
public static class WordingPaths
{
    public const string AppFolderName = "Wording";

    public const string DataFileName = "words.json";

    /// <summary>Name of the legacy XML file, used for the one-off import.</summary>
    public const string LegacyDataFileName = "WordsData.xml";

    /// <summary>
    /// %APPDATA%\Wording on Windows, ~/Library/Application Support/Wording on macOS.
    /// </summary>
    public static string DataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            // On macOS SpecialFolder.ApplicationData points at ~/.config, while the
            // native location for application data is Library/Application Support.
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
