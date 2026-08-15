namespace Wording.Core.Learning;

/// <summary>
/// How well the user recalled a word. The values are SM-2 quality scores (0-5)
/// and are passed straight to <see cref="SpacedRepetitionScheduler"/>.
/// Three levels, because that is as many buttons as fit in a notification.
/// </summary>
public enum ReviewGrade
{
    /// <summary>Forgotten - the repetition count starts over.</summary>
    Again = 0,

    /// <summary>Recalled, but with effort - the interval grows, the ease factor drops.</summary>
    Hard = 3,

    /// <summary>Recalled without hesitation.</summary>
    Good = 5,
}
