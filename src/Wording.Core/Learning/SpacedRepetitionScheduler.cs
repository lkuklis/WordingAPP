namespace Wording.Core.Learning;

/// <summary>
/// Algorytm SM-2 (SuperMemo 2) w wersji czystej funkcji.
/// Liczy nowy stan powtorek na podstawie poprzedniego stanu i oceny.
/// </summary>
public static class SpacedRepetitionScheduler
{
    /// <summary>Odstep po pierwszej udanej powtorce.</summary>
    const double FirstIntervalDays = 1.0;

    /// <summary>Odstep po drugiej udanej powtorce.</summary>
    const double SecondIntervalDays = 6.0;

    /// <summary>
    /// Po <see cref="ReviewGrade.Again"/> slowko wraca po chwili, a nie natychmiast -
    /// inaczej zablokowaloby cala rotacje, bo bylo by stale najbardziej przeterminowane.
    /// </summary>
    static readonly TimeSpan RelearnDelay = TimeSpan.FromMinutes(10);

    public static ReviewState Apply(ReviewState current, ReviewGrade grade, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);

        var quality = (int)grade;
        var easeFactor = NextEaseFactor(current.EaseFactor, quality);

        // SM-2 traktuje ocene ponizej 3 jako nietrafiona - licznik powtorek startuje od zera.
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
    /// Oryginalny wzor SM-2 na korekte latwosci:
    /// EF' = EF + (0.1 - (5-q) * (0.08 + (5-q) * 0.02)), z dolnym progiem 1.3.
    /// </summary>
    static double NextEaseFactor(double easeFactor, int quality)
    {
        var delta = 5 - quality;
        var updated = easeFactor + (0.1 - delta * (0.08 + delta * 0.02));

        return Math.Max(ReviewState.MinimumEaseFactor, updated);
    }
}
