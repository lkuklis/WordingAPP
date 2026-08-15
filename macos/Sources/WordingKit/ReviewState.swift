import Foundation

extension TimeInterval {
    /// A day in seconds. Shared by the scheduler and the selector so neither has to
    /// reach into the other for a unit conversion.
    public static let day: TimeInterval = 24 * 60 * 60
}

/// How well the user recalled a word. The values are SM-2 quality scores (0-5),
/// matching the .NET side.
public enum ReviewGrade: Int, Sendable {
    /// Forgotten - the repetition count starts over.
    case again = 0
    /// Recalled, but with effort.
    case hard = 3
    /// Recalled without hesitation.
    case good = 5
}

/// Review state of a single word.
///
/// Immutable - `SpacedRepetitionScheduler` returns a new state instead of mutating
/// this one, which keeps the algorithm a pure function.
public struct ReviewState: Codable, Equatable, Sendable {
    /// Starting ease factor, per SM-2.
    public static let defaultEaseFactor = 2.5

    /// Lower bound on the ease factor, per SM-2.
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

    /// State of a freshly added word: due immediately.
    public static func new(now: Date) -> ReviewState {
        ReviewState(dueUtc: now)
    }
}
