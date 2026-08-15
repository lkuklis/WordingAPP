import Foundation

/// Ocena, jaka uzytkownik wystawia sobie po zobaczeniu slowka.
/// Wartosci odpowiadaja skali jakosci SM-2 (0-5), tak samo jak w wersji .NET.
public enum ReviewGrade: Int, Sendable, CaseIterable {
    /// Nie pamietam - powtorki startuja od nowa.
    case again = 0
    /// Z trudem, ale trafione.
    case hard = 3
    /// Pamietam bez wahania.
    case good = 5
}

/// Stan powtorek pojedynczego slowka.
///
/// Niemutowalny - `SpacedRepetitionScheduler` zwraca nowy stan zamiast
/// modyfikowac istniejacy, dzieki czemu algorytm jest czysta funkcja.
public struct ReviewState: Codable, Equatable, Sendable {
    /// Startowa latwosc wg SM-2.
    public static let defaultEaseFactor = 2.5

    /// Dolny prog latwosci wg SM-2.
    public static let minimumEaseFactor = 1.3

    public var repetitions: Int
    public var intervalDays: Double
    public var easeFactor: Double
    public var dueUtc: Date
    public var lastReviewedUtc: Date?
    public var lapses: Int

    public init(
        repetitions: Int = 0,
        intervalDays: Double = 0,
        easeFactor: Double = ReviewState.defaultEaseFactor,
        dueUtc: Date,
        lastReviewedUtc: Date? = nil,
        lapses: Int = 0
    ) {
        self.repetitions = repetitions
        self.intervalDays = intervalDays
        self.easeFactor = easeFactor
        self.dueUtc = dueUtc
        self.lastReviewedUtc = lastReviewedUtc
        self.lapses = lapses
    }

    /// Stan swiezo dodanego slowka: wymagalne natychmiast.
    public static func new(now: Date) -> ReviewState {
        ReviewState(dueUtc: now)
    }
}
