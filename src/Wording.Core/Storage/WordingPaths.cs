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

    /// <summary>Imported sets live beside words.json, one file each, never inside it.</summary>
    public const string SetsFolderName = "sets";

    /// <summary>
    /// Copies taken before a destructive change, in a subdirectory of whichever file
    /// they belong to. A subdirectory rather than a sibling file on purpose: the set
    /// catalogue lists *.json directly inside sets/ and would otherwise show every
    /// backup as a set of its own.
    /// </summary>
    public const string BackupsFolderName = "backups";

    public static string SetsDirectory() => Path.Combine(DataDirectory(), SetsFolderName);

    /// <summary>
    /// The file an imported set is written to. The identifier has already been through
    /// <c>PackSlug</c>, which is what stops a downloaded file from choosing its own path.
    /// </summary>
    public static string SetFile(string slug, string? setsDirectory = null) =>
        Path.Combine(setsDirectory ?? SetsDirectory(), slug + ".json");
}
