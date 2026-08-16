using Microsoft.Extensions.Configuration;
using Wording.Core.Storage;

namespace Wording.WordApp;

/// <summary>
/// Application settings from appsettings.json. Only the app layer reads configuration -
/// Wording.Core receives ready-made values.
/// </summary>
public sealed class WordingSettings
{
    public const string SectionName = "wording";

    /// <summary>
    /// How many seconds between words. Matches the macOS default.
    /// <para>
    /// This used to be 5, which was a value for watching the thing work, not for using
    /// it: Windows keeps a toast up for several seconds and queues the rest, so a word
    /// every five seconds built a backlog that went on appearing minutes after the app
    /// had been closed.
    /// </para>
    /// </summary>
    public int ChangeTimeSeconds { get; set; } = 30;

    /// <summary>How long the notification should stay visible.</summary>
    public int ShowTimeSeconds { get; set; } = 6;

    /// <summary>Overrides the data file path. Empty means the per-user data directory.</summary>
    public string? DataFile { get; set; }

    public static WordingSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var settings = new WordingSettings();
        configuration.GetSection(SectionName).Bind(settings);

        // Guards against a zero or negative interval in a hand-edited file.
        settings.ChangeTimeSeconds = Math.Max(1, settings.ChangeTimeSeconds);
        settings.ShowTimeSeconds = Math.Max(1, settings.ShowTimeSeconds);

        return settings;
    }

    /// <summary>The data file this run will use.</summary>
    public string ResolveDataFile() =>
        string.IsNullOrWhiteSpace(DataFile) ? WordingPaths.DataFile() : DataFile;

    /// <summary>
    /// The legacy XML file, if one exists. Looks next to the executable (where the
    /// starter pack ships) and in the working directory, where the old version kept it.
    /// </summary>
    public static string? FindLegacyXml()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName),
            Path.Combine(Directory.GetCurrentDirectory(), WordingPaths.LegacyDataFileName),
        ];

        return Array.Find(candidates, File.Exists);
    }
}
