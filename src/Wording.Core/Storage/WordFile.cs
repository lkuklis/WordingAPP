namespace Wording.Core.Storage;

/// <summary>
/// Shape of words.json. Split out of <see cref="JsonWordStore"/> because the
/// System.Text.Json source generator needs a top-level type.
/// </summary>
internal sealed class WordFile
{
    public int Version { get; set; } = JsonWordStore.CurrentVersion;

    public List<Word> Words { get; set; } = [];
}
