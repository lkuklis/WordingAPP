namespace Wording.Core.Storage;

/// <summary>
/// Header of an imported set, stored inside its own file.
/// <para>
/// Absent from words.json: the user's own words are not an import and have no source.
/// </para>
/// </summary>
public sealed class WordSet
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Where it came from, so the set can be refreshed later.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Carried over from the pack - see <see cref="Packs.PackKind"/>.</summary>
    public string? Kind { get; set; }

    public DateTimeOffset ImportedUtc { get; set; }
}

/// <summary>
/// One entry in the list of installed sets. The word count is read from the file rather
/// than stored in it - a stored count starts lying the moment a word is deleted.
/// </summary>
public sealed record WordSetInfo(
    string Id,
    string Name,
    string? SourceUrl,
    string Kind,
    int WordCount,
    string Path);
