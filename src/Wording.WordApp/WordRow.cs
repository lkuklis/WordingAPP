using Wording.Core;

namespace Wording.WordApp;

/// <summary>
/// Wiersz listy slowek, wspolny dla powloki WinForms i Avalonii.
/// Osobny typ, bo <see cref="Word"/> niesie zagniezdzony stan powtorek,
/// ktorego zadna z siatek nie pokaze sensownie sama z siebie.
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
        NextReview = OpiszTermin(word, now);
    }

    public Guid Id { get; }

    public string Word { get; }

    public string Translation { get; }

    public int Reviews { get; }

    public int Lapses { get; }

    public string NextReview { get; }

    static string OpiszTermin(Word word, DateTimeOffset now)
    {
        if (word.Review.LastReviewedUtc is null)
        {
            return "new";
        }

        var doTerminu = word.Review.DueUtc - now;

        if (doTerminu <= TimeSpan.Zero)
        {
            return "due";
        }

        if (doTerminu < TimeSpan.FromHours(1))
        {
            return $"in {Math.Max(1, (int)doTerminu.TotalMinutes)} min";
        }

        if (doTerminu < TimeSpan.FromDays(1))
        {
            return $"in {(int)doTerminu.TotalHours} h";
        }

        return $"in {(int)doTerminu.TotalDays} d";
    }
}
