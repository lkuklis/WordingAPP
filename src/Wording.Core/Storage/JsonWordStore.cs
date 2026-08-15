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
    WordSet? _set;

    /// <summary>
    /// The set header when this file is an imported set, null for the user's own words.
    /// Held so that saving a grade cannot quietly drop it.
    /// </summary>
    public WordSet? Set => _set;

    /// <summary>Mirrors WordStore.fileURL in the Swift port.</summary>
    public string FilePath => _path;

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
            _set = null;
            return;
        }

        var json = File.ReadAllText(_path);

        if (string.IsNullOrWhiteSpace(json))
        {
            _words = [];
            _set = null;
            return;
        }

        var file = JsonSerializer.Deserialize(json, WordJsonContext.Default.WordFile);

        _words = file?.Words ?? [];
        _set = file?.Set;
    }

    /// <summary>Marks this file as an imported set, or refreshes the header of one.</summary>
    public void Describe(WordSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        _set = set;
        Save();
    }

    /// <summary>
    /// Appends several words in one save. Importing a pack one <see cref="Add"/> at a
    /// time would rewrite the whole file per word.
    /// </summary>
    internal void AddRange(IEnumerable<Word> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        _words.AddRange(words);
        Save();
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

    /// <summary>
    /// Deletes every word, after copying the file aside.
    /// <para>
    /// The copy is not optional politeness: this throws away review progress that can
    /// take months to build and that nothing else in the app can reconstruct. Each
    /// backup is stamped with the time rather than overwriting one fixed name - clearing
    /// an already-cleared store twice would otherwise replace the useful backup with a
    /// copy of nothing.
    /// </para>
    /// </summary>
    /// <returns>Path of the backup, or null when there was nothing to delete.</returns>
    public string? RemoveAll()
    {
        if (_words.Count == 0)
        {
            return null;
        }

        var backup = BackupPath();

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup)!);
        File.Copy(_path, backup, overwrite: true);

        _words = [];
        Save();

        return backup;
    }

    string BackupPath()
    {
        var directory = System.IO.Path.GetDirectoryName(_path) ?? ".";
        var stem = System.IO.Path.GetFileNameWithoutExtension(_path);
        var stamp = _clock.GetUtcNow().ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);

        return System.IO.Path.Combine(
            directory,
            WordingPaths.BackupsFolderName,
            $"{stem}-{stamp}.json");
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
            new WordFile { Set = _set, Words = _words },
            WordJsonContext.Default.WordFile);

        // Write next to the target, then swap it in - a crash mid-write leaves the
        // original untouched.
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, _path, overwrite: true);
    }
}
