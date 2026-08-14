namespace Wording.Core.Storage;

/// <summary>
/// Kształt pliku words.json. Wydzielony z <see cref="JsonWordStore"/>, bo generator
/// zrodel dla System.Text.Json potrzebuje typu najwyzszego poziomu.
/// </summary>
internal sealed class WordFile
{
    public int Version { get; set; } = JsonWordStore.CurrentVersion;

    public List<Word> Words { get; set; } = [];
}
