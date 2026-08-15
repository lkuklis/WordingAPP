using System;
using Wording.Core;

namespace Wording.WordApp;

/// <summary>
/// Wiersz siatki. Osobny typ, bo <see cref="Word"/> niesie zagniezdzony stan
/// powtorek, ktorego DataGridView nie pokaze sam z siebie.
/// </summary>
public sealed class WordRow
{
    public WordRow(Word word, DateTimeOffset now)
    {
        Id = word.Id;
        Word = word.Original;
        Translation = word.Translation;
        Reviews = word.Review.Repetitions;
        Lapses = word.Review.Lapses;
        NextReview = DescribeDue(word, now);
    }

    public Guid Id { get; }

    public string Word { get; }

    public string Translation { get; }

    public int Reviews { get; }

    public int Lapses { get; }

    public string NextReview { get; }

    static string DescribeDue(Word word, DateTimeOffset now)
    {
        if (word.IsNew)
        {
            return "new";
        }

        if (word.IsDue(now))
        {
            return "due";
        }

        var remaining = word.Review.DueUtc - now;

        if (remaining < TimeSpan.FromHours(1))
        {
            return $"in {Math.Max(1, (int)remaining.TotalMinutes)} min";
        }

        if (remaining < TimeSpan.FromDays(1))
        {
            return $"in {(int)remaining.TotalHours} h";
        }

        return $"in {(int)remaining.TotalDays} d";
    }
}
