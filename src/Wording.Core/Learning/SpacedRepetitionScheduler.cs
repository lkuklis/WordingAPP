namespace Wording.Core.Learning;

/// <summary>
/// The SM-2 (SuperMemo 2) algorithm as a pure function: it computes the next review
/// state from the previous state and a grade.
/// </summary>
public static class SpacedRepetitionScheduler
{
    /// <summary>Interval after the first successful review.</summary>
    const double FirstIntervalDays = 1.0;

    /// <summary>Interval after the second successful review.</summary>
    const double SecondIntervalDays = 6.0;

    /// <summary>
    /// After <see cref="ReviewGrade.Again"/> a word comes back shortly, not instantly -
    /// otherwise it would be permanently the most overdue word and would block the rotation.
    /// </summary>
    static readonly TimeSpan RelearnDelay = TimeSpan.FromMinutes(10);

    public static ReviewState Apply(ReviewState current, ReviewGrade grade, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);

        var quality = (int)grade;
        var easeFactor = NextEaseFactor(current.EaseFactor, quality);

        // SM-2 treats anything below 3 as a failed recall - the repetition count restarts.
        if (quality < 3)
        {
            return current with
            {
                Repetitions = 0,
                IntervalDays = 0,
                EaseFactor = easeFactor,
                DueUtc = now + RelearnDelay,
                LastReviewedUtc = now,
                Lapses = current.Lapses + 1,
            };
        }

        var repetitions = current.Repetitions + 1;
        var interval = repetitions switch
        {
            1 => FirstIntervalDays,
            2 => SecondIntervalDays,
            _ => current.IntervalDays * easeFactor,
        };

        return current with
        {
            Repetitions = repetitions,
            IntervalDays = interval,
            EaseFactor = easeFactor,
            DueUtc = now + TimeSpan.FromDays(interval),
            LastReviewedUtc = now,
        };
    }

    /// <summary>
    /// The original SM-2 ease adjustment:
    /// EF' = EF + (0.1 - (5-q) * (0.08 + (5-q) * 0.02)), floored at 1.3.
    /// </summary>
    static double NextEaseFactor(double easeFactor, int quality)
    {
        var delta = 5 - quality;
        var updated = easeFactor + (0.1 - delta * (0.08 + delta * 0.02));

        return Math.Max(ReviewState.MinimumEaseFactor, updated);
    }
}
