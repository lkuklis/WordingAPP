using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core;

/// <summary>
/// Fasada dla warstwy UI: lista slowek, dodawanie, usuwanie, wybor nastepnego
/// slowka do pokazania i ocena powtorki.
/// <para>
/// Magazyn przychodzi przez konstruktor, zeby caly proces wspoldzielil jedna
/// instancje - inaczej kazdy ekran pisalby przez wlasna kopie danych w pamieci.
/// </para>
/// </summary>
public sealed class WordManager
{
    readonly JsonWordStore _store;
    readonly TimeProvider _clock;
    readonly Random _random;

    public WordManager(JsonWordStore store, TimeProvider? clock = null, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _clock = clock ?? TimeProvider.System;
        _random = random ?? Random.Shared;
    }

    public IReadOnlyList<Word> GetWords() => _store.GetAll();

    /// <exception cref="ArgumentException">Gdy ktorakolwiek ze stron jest pusta.</exception>
    public Word AddWord(string original, string translation)
    {
        var trimmedOriginal = (original ?? string.Empty).Trim();
        var trimmedTranslation = (translation ?? string.Empty).Trim();

        if (trimmedOriginal.Length == 0)
        {
            throw new ArgumentException("Slowko nie moze byc puste.", nameof(original));
        }

        if (trimmedTranslation.Length == 0)
        {
            throw new ArgumentException("Tlumaczenie nie moze byc puste.", nameof(translation));
        }

        return _store.Add(trimmedOriginal, trimmedTranslation);
    }

    public bool RemoveWord(Guid id) => _store.Remove(id);

    /// <summary>Slowko, ktore powinno teraz trafic do powiadomienia. Null, gdy lista jest pusta.</summary>
    public Word? NextWordToShow() =>
        WordSelector.PickNext(_store.GetAll(), _clock.GetUtcNow(), _random);

    /// <summary>Zapisuje ocene powtorki i przelicza termin nastepnego pokazania.</summary>
    /// <returns>False, gdy slowka o takim id juz nie ma.</returns>
    public bool Grade(Guid id, ReviewGrade grade)
    {
        var word = _store.GetById(id);

        if (word is null)
        {
            return false;
        }

        word.Review = SpacedRepetitionScheduler.Apply(word.Review, grade, _clock.GetUtcNow());

        return _store.Update(word);
    }
}
