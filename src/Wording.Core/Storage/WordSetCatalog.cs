using System.Text.Json;

namespace Wording.Core.Storage;

/// <summary>
/// Lists the imported sets by reading the directory.
/// <para>
/// There is deliberately no registry file listing them. A registry has to be kept in
/// step with the disk and silently stops matching it the moment a file is moved or
/// deleted by hand; the directory cannot disagree with itself.
/// </para>
/// <para>
/// The user's own words.json is not included: it is not an import and has no header.
/// Naming it is the UI's job.
/// </para>
/// </summary>
public static class WordSetCatalog
{
    /// <summary>
    /// The file the app should open for a remembered set choice.
    /// <para>
    /// Falls back to the user's own words whenever that choice cannot be honoured - no
    /// choice made, an identifier that is not a safe slug, or a set deleted from disk
    /// since it was chosen. Refusing to start because a remembered set has gone would
    /// leave the user with an app that will not open.
    /// </para>
    /// </summary>
    public static string ResolveActiveFile(
        string? setId,
        string? dataFile = null,
        string? setsDirectory = null)
    {
        var ownWords = dataFile ?? WordingPaths.DataFile();

        if (!Packs.PackSlug.TryNormalize(setId, out var slug))
        {
            return ownWords;
        }

        var path = WordingPaths.SetFile(slug, setsDirectory);

        return File.Exists(path) ? path : ownWords;
    }

    public static IReadOnlyList<WordSetInfo> List(string? setsDirectory = null)
    {
        var directory = setsDirectory ?? WordingPaths.SetsDirectory();

        if (!Directory.Exists(directory))
        {
            return [];
        }

        var sets = new List<WordSetInfo>();

        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            if (Read(path) is { } set)
            {
                sets.Add(set);
            }
        }

        return [.. sets.OrderBy(set => set.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>
    /// Reads one set, or null when the file cannot be understood. A damaged file is
    /// left out of the list rather than taking the whole list down with it.
    /// </summary>
    public static WordSetInfo? Read(string path)
    {
        WordFile? file;

        try
        {
            file = JsonSerializer.Deserialize(File.ReadAllText(path), WordJsonContext.Default.WordFile);
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (file is null)
        {
            return null;
        }

        // The file name is the identity, not the header: they can disagree if the file
        // was renamed by hand, and the name on disk is the one that decides which file
        // a refresh or a delete would touch.
        var id = System.IO.Path.GetFileNameWithoutExtension(path);

        return new WordSetInfo(
            Id: id,
            Name: string.IsNullOrWhiteSpace(file.Set?.Name) ? id : file.Set.Name,
            SourceUrl: file.Set?.SourceUrl,
            Kind: Packs.PackKind.Normalize(file.Set?.Kind),
            WordCount: file.Words.Count,
            Path: path);
    }
}
