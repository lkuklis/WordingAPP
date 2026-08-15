using System.Text.Json;
using Wording.Core.Learning;

namespace Wording.Core.Storage;

/// <summary>
/// Stores words in a JSON file. Keeps the list in memory and saves after every change.
/// <para>
/// Saving is atomic (temporary file, then replace), so interrupting the process
/// mid-write cannot leave the user with a truncated data file.
/// </para>
/// </summary>
public sealed class JsonWordStore
{
    internal const int CurrentVersion = 1;

    readonly string _path;
    readonly TimeProvider _clock;
    List<Word> _words = [];

    public JsonWordStore(string path, TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _path = path;
        _clock = clock ?? TimeProvider.System;

        Reload();
    }

    /// <summary>Re-reads from disk, discarding the in-memory state.</summary>
    public void Reload()
    {
        if (!File.Exists(_path))
        {
            _words = [];
            return;
        }

        var json = File.ReadAllText(_path);

        if (string.IsNullOrWhiteSpace(json))
        {
            _words = [];
            return;
        }

        _words = JsonSerializer.Deserialize(json, WordJsonContext.Default.WordFile)?.Words ?? [];
    }

    /// <summary>
    /// Imports words from the legacy XML format, but only into an empty store -
    /// it never overwrites data that is already there.
    /// </summary>
    /// <returns>How many words were imported.</returns>
    public int ImportLegacyIfEmpty(string? legacyXmlPath)
    {
        if (_words.Count > 0 || legacyXmlPath is null || !File.Exists(legacyXmlPath))
        {
            return 0;
        }

        _words = [.. LegacyXmlImporter.Read(legacyXmlPath, _clock.GetUtcNow())];
        Save();

        return _words.Count;
    }

    public IReadOnlyList<Word> GetAll() => _words;

    public Word? GetById(Guid id) => _words.Find(word => word.Id == id);

    public Word Add(string original, string translation)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(translation);

        var now = _clock.GetUtcNow();
        var word = new Word
        {
            Original = original,
            Translation = translation,
            CreatedUtc = now,
            Review = ReviewState.New(now),
        };

        _words.Add(word);
        Save();

        return word;
    }

    public bool Remove(Guid id)
    {
        if (_words.RemoveAll(word => word.Id == id) == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Persists changes to a word already in the store (for example after grading).</summary>
    public bool Update(Word word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var index = _words.FindIndex(existing => existing.Id == word.Id);

        if (index < 0)
        {
            return false;
        }

        _words[index] = word;
        Save();

        return true;
    }

    void Save()
    {
        var directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            new WordFile { Words = _words },
            WordJsonContext.Default.WordFile);

        // Write next to the target, then swap it in - a crash mid-write leaves the
        // original untouched.
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, _path, overwrite: true);
    }
}
