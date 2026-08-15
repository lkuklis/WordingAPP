namespace Wording.Core.Learning;

/// <summary>
/// Review state of a single word. Immutable - <see cref="SpacedRepetitionScheduler"/>
/// returns a new state instead of mutating this one, which keeps the algorithm a pure
/// function that can be tested without any surrounding context.
/// </summary>
public sealed record ReviewState
{
    /// <summary>Starting ease factor, per SM-2.</summary>
    public const double DefaultEaseFactor = 2.5;

    /// <summary>Lower bound on the ease factor, per SM-2 - below this a word would come back too often.</summary>
    public const double MinimumEaseFactor = 1.3;

    /// <summary>Consecutive successful reviews. Reset by <see cref="ReviewGrade.Again"/>.</summary>
    public int Repetitions { get; init; }

    /// <summary>Current gap between reviews, in days.</summary>
    public double IntervalDays { get; init; }

    /// <summary>SM-2 ease factor - the higher it is, the faster the intervals grow.</summary>
    public double EaseFactor { get; init; } = DefaultEaseFactor;

    /// <summary>The moment from which the word should show up again.</summary>
    public DateTimeOffset DueUtc { get; init; }

    /// <summary>When it was last graded. Null for a word that has never been reviewed.</summary>
    public DateTimeOffset? LastReviewedUtc { get; init; }

    /// <summary>How many times the word was forgotten. Informational only; it does not feed the algorithm.</summary>
    public int Lapses { get; init; }

    /// <summary>State of a freshly added word: due immediately, so it enters rotation at once.</summary>
    public static ReviewState New(DateTimeOffset now) => new() { DueUtc = now };
}
