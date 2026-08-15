using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core;

/// <summary>
/// The façade the UI talks to: list words, add, remove, pick the next word to show,
/// and grade a review.
/// <para>
/// The store is injected so the whole process shares one instance - otherwise each
/// screen would write through its own in-memory copy.
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

    /// <exception cref="ArgumentException">When either side is empty.</exception>
    public Word AddWord(string original, string translation)
    {
        var trimmedOriginal = (original ?? string.Empty).Trim();
        var trimmedTranslation = (translation ?? string.Empty).Trim();

        // The message is deliberately terse: the UI owns the user-facing wording.
        if (trimmedOriginal.Length == 0)
        {
            throw new ArgumentException("The word must not be empty.", nameof(original));
        }

        if (trimmedTranslation.Length == 0)
        {
            throw new ArgumentException("The translation must not be empty.", nameof(translation));
        }

        return _store.Add(trimmedOriginal, trimmedTranslation);
    }

    public bool RemoveWord(Guid id) => _store.Remove(id);

    /// <summary>The word that should go into the notification now. Null when the list is empty.</summary>
    public Word? NextWordToShow() =>
        WordSelector.PickNext(_store.GetAll(), _clock.GetUtcNow(), _random);

    /// <summary>Records a review grade and recomputes when the word is due next.</summary>
    /// <returns>False when no word with that id exists any more.</returns>
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
