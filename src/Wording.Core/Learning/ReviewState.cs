namespace Wording.Core.Learning;

/// <summary>
/// Stan powtorek pojedynczego slowka. Niemutowalny - <see cref="SpacedRepetitionScheduler"/>
/// zwraca nowy stan zamiast modyfikowac istniejacy, dzieki czemu algorytm jest
/// czysta funkcja i daje sie testowac bez zadnego kontekstu.
/// </summary>
public sealed record ReviewState
{
    /// <summary>Startowa latwosc wg SM-2.</summary>
    public const double DefaultEaseFactor = 2.5;

    /// <summary>Dolny prog latwosci wg SM-2 - ponizej tego slowko wracaloby zbyt czesto.</summary>
    public const double MinimumEaseFactor = 1.3;

    /// <summary>Liczba udanych powtorek z rzedu. Zerowana przy <see cref="ReviewGrade.Again"/>.</summary>
    public int Repetitions { get; init; }

    /// <summary>Aktualny odstep miedzy powtorkami w dniach.</summary>
    public double IntervalDays { get; init; }

    /// <summary>Wspolczynnik latwosci SM-2 - im wyzszy, tym szybciej rosna odstepy.</summary>
    public double EaseFactor { get; init; } = DefaultEaseFactor;

    /// <summary>Moment, od ktorego slowko powinno sie znow pojawiac.</summary>
    public DateTimeOffset DueUtc { get; init; }

    /// <summary>Kiedy ostatnio ocenione. Null dla slowka jeszcze nie powtarzanego.</summary>
    public DateTimeOffset? LastReviewedUtc { get; init; }

    /// <summary>Ile razy slowko zostalo zapomniane. Czysto informacyjne, nie wplywa na algorytm.</summary>
    public int Lapses { get; init; }

    /// <summary>
    /// Stan swiezo dodanego slowka: wymagalne natychmiast, zeby od razu weszlo do rotacji.
    /// </summary>
    public static ReviewState New(DateTimeOffset now) => new() { DueUtc = now };
}
