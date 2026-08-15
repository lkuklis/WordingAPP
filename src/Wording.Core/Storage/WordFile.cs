namespace Wording.Core.Storage;

/// <summary>
/// Shape of words.json. Split out of <see cref="JsonWordStore"/> because the
/// System.Text.Json source generator needs a top-level type.
/// </summary>
internal sealed class WordFile
{
    public int Version { get; set; } = JsonWordStore.CurrentVersion;

    /// <summary>Present only in an imported set; null in the user's own words.json.</summary>
    public WordSet? Set { get; set; }

    public List<Word> Words { get; set; } = [];
}
