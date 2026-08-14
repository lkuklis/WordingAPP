using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core;

/// <summary>
/// Fasada, z ktorej korzysta warstwa UI: lista slowek, dodawanie, usuwanie,
/// wybor nastepnego slowka do pokazania i ocena powtorki.
/// <para>
/// Magazyn wstrzykiwany jest przez konstruktor. To celowe: wczesniej kazdy
/// ekran tworzyl wlasne repozytorium, wiec okno glowne i okienko dodawania
/// pisaly przez osobne kopie danych w pamieci i musialy sie recznie odswiezac.
/// Teraz caly proces wspoldzieli jedna instancje.
/// </para>
/// </summary>
public sealed class WordManager
{
    readonly IWordStore _store;
    readonly WordSelector _selector;
    readonly TimeProvider _clock;

    public WordManager(IWordStore store, TimeProvider? clock = null, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _clock = clock ?? TimeProvider.System;
        _selector = new WordSelector(_clock, random);
    }

    public IReadOnlyList<Word> GetWords() => _store.GetAll();

    public Word? GetWord(Guid id) => _store.GetById(id);

    /// <exception cref="ArgumentException">Gdy ktorakolwiek ze stron jest pusta.</exception>
    public Word AddWord(string original, string translation)
    {
        var oryginal = (original ?? string.Empty).Trim();
        var tlumaczenie = (translation ?? string.Empty).Trim();

        if (oryginal.Length == 0)
        {
            throw new ArgumentException("Slowko nie moze byc puste.", nameof(original));
        }

        if (tlumaczenie.Length == 0)
        {
            throw new ArgumentException("Tlumaczenie nie moze byc puste.", nameof(translation));
        }

        return _store.Add(oryginal, tlumaczenie);
    }

    public bool RemoveWord(Guid id) => _store.Remove(id);

    /// <summary>
    /// Slowko, ktore powinno teraz trafic do powiadomienia. Null, gdy lista jest pusta.
    /// </summary>
    public Word? NextWordToShow() => _selector.PickNext(_store.GetAll());

    /// <summary>
    /// Zapisuje ocene powtorki i przelicza termin nastepnego pokazania.
    /// </summary>
    /// <returns>False, gdy slowka o takim id juz nie ma.</returns>
    public bool Grade(Guid id, ReviewGrade grade)
    {
        var slowo = _store.GetById(id);

        if (slowo is null)
        {
            return false;
        }

        slowo.Review = SpacedRepetitionScheduler.Apply(slowo.Review, grade, _clock.GetUtcNow());

        return _store.Update(slowo);
    }

    public void Reload() => _store.Reload();
}
