namespace Wording.Core.Learning;

/// <summary>
/// Picks the next word to show.
/// <para>
/// It deliberately does NOT gate on the due date the way a conventional SRS would.
/// This app shows a word every few minutes in the background rather than in review
/// sessions, so due-date gating would leave it with nothing to display most of the
/// time. Instead every word gets a weight: overdue words dominate, well-known ones
/// appear rarely, but nothing ever leaves the rotation entirely.
/// </para>
/// </summary>
public static class WordSelector
{
    /// <summary>A word that has never been graded should show often, so it enters rotation quickly.</summary>
    internal const double NewWordWeight = 10.0;

    /// <summary>Weight of a word exactly at its due date. Each day of delay adds 1.</summary>
    internal const double DueWeight = 1.0;

    /// <summary>Cap on lateness, so one word forgotten a year ago cannot dominate the rotation.</summary>
    internal const double MaxOverdueDays = 30.0;

    /// <summary>Floor for a just-reviewed word - small, but non-zero, so nothing drops out of rotation.</summary>
    internal const double MinWeight = 0.02;

    /// <summary>Returns a word to show, or null when the list is empty.</summary>
    public static Word? PickNext(IReadOnlyList<Word> words, DateTimeOffset now, Random random)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(random);

        if (words.Count == 0)
        {
            return null;
        }

        var weights = new double[words.Count];
        var total = 0.0;

        for (var i = 0; i < words.Count; i++)
        {
            weights[i] = Weight(words[i], now);
            total += weights[i];
        }

        var roll = random.NextDouble() * total;

        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
            {
                return words[i];
            }
        }

        // Unreachable except for rounding error in the floating-point sum.
        return words[^1];
    }

    /// <summary>Weight of a word: the more urgent it is, the likelier it is to be drawn.</summary>
    internal static double Weight(Word word, DateTimeOffset now)
    {
        if (word.IsNew)
        {
            return NewWordWeight;
        }

        var overdueDays = (now - word.Review.DueUtc).TotalDays;

        if (overdueDays >= 0)
        {
            return DueWeight + Math.Min(overdueDays, MaxOverdueDays);
        }

        // Not due yet - the weight shrinks the further away the due date is.
        return Math.Max(MinWeight, DueWeight / (1 - overdueDays));
    }
}
