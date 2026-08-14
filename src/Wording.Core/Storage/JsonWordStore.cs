using System.Text.Json;
using Wording.Core.Learning;

namespace Wording.Core.Storage;

/// <summary>
/// Magazyn slowek w pliku JSON. Trzyma liste w pamieci i zapisuje po kazdej zmianie.
/// <para>
/// Zapis jest atomowy (plik tymczasowy + podmiana), zeby przerwanie procesu w trakcie
/// zapisu nie zostawilo uciętego pliku z danymi uzytkownika.
/// </para>
/// </summary>
public sealed class JsonWordStore : IWordStore
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

    /// <summary>Sciezka pliku, z ktorego korzysta ten magazyn.</summary>
    public string Path => _path;

    /// <summary>
    /// Otwiera magazyn, a gdy pliku JSON jeszcze nie ma i wskazano istniejacy plik
    /// w starym formacie XML - przenosi z niego slowka i od razu zapisuje.
    /// </summary>
    public static JsonWordStore OpenOrMigrate(string jsonPath, string? legacyXmlPath, TimeProvider? clock = null)
    {
        var zegar = clock ?? TimeProvider.System;
        var magazyn = new JsonWordStore(jsonPath, zegar);

        var trzebaMigrowac = magazyn._words.Count == 0
            && !File.Exists(jsonPath)
            && legacyXmlPath is not null
            && File.Exists(legacyXmlPath);

        if (trzebaMigrowac)
        {
            magazyn._words = [.. LegacyXmlImporter.Read(legacyXmlPath!, zegar.GetUtcNow())];
            magazyn.Save();
        }

        return magazyn;
    }

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

        var plik = JsonSerializer.Deserialize(json, WordJsonContext.Default.WordFile);
        _words = plik?.Words ?? [];
    }

    public IReadOnlyList<Word> GetAll() => _words;

    public Word? GetById(Guid id) => _words.Find(word => word.Id == id);

    public Word Add(string original, string translation)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(translation);

        var teraz = _clock.GetUtcNow();
        var slowo = new Word
        {
            Original = original,
            Translation = translation,
            CreatedUtc = teraz,
            Review = ReviewState.New(teraz),
        };

        _words.Add(slowo);
        Save();

        return slowo;
    }

    public bool Remove(Guid id)
    {
        var usuniete = _words.RemoveAll(word => word.Id == id);

        if (usuniete == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    public bool Update(Word word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var index = _words.FindIndex(istniejace => istniejace.Id == word.Id);

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
        var katalog = System.IO.Path.GetDirectoryName(_path);

        if (!string.IsNullOrEmpty(katalog))
        {
            Directory.CreateDirectory(katalog);
        }

        var json = JsonSerializer.Serialize(
            new WordFile { Version = CurrentVersion, Words = _words },
            WordJsonContext.Default.WordFile);

        // Zapis do pliku obok, potem podmiana - dzieki temu w razie awarii
        // w trakcie zapisu oryginal zostaje nietkniety.
        var tymczasowy = _path + ".tmp";
        File.WriteAllText(tymczasowy, json);
        File.Move(tymczasowy, _path, overwrite: true);
    }
}
