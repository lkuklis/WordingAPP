import Foundation

/// The SM-2 (SuperMemo 2) algorithm as a pure function.
///
/// A port of `Wording.Core.Learning.SpacedRepetitionScheduler` - both implementations
/// must produce identical numbers, because they work on the same data file.
public enum SpacedRepetitionScheduler {
    /// Interval after the first successful review.
    static let firstIntervalDays = 1.0

    /// Interval after the second successful review.
    static let secondIntervalDays = 6.0

    /// After `again` a word comes back shortly, not instantly - otherwise it would be
    /// permanently the most overdue word and would block the whole rotation.
    static let relearnDelay: TimeInterval = 10 * 60

    public static func apply(_ current: ReviewState, grade: ReviewGrade, now: Date) -> ReviewState {
        let quality = grade.rawValue
        let easeFactor = nextEaseFactor(current.easeFactor, quality: quality)

        // SM-2 treats anything below 3 as a failed recall.
        guard quality >= 3 else {
            return ReviewState(
                repetitions: 0,
                intervalDays: 0,
                easeFactor: easeFactor,
                dueUtc: now.addingTimeInterval(relearnDelay),
                lastReviewedUtc: now,
                lapses: current.lapses + 1
            )
        }

        let repetitions = current.repetitions + 1
        let interval: Double =
            switch repetitions {
            case 1: firstIntervalDays
            case 2: secondIntervalDays
            default: current.intervalDays * easeFactor
            }

        return ReviewState(
            repetitions: repetitions,
            intervalDays: interval,
            easeFactor: easeFactor,
            dueUtc: now.addingTimeInterval(interval * .day),
            lastReviewedUtc: now,
            lapses: current.lapses
        )
    }

    /// The original SM-2 formula: EF' = EF + (0.1 - (5-q) * (0.08 + (5-q) * 0.02)).
    static func nextEaseFactor(_ easeFactor: Double, quality: Int) -> Double {
        let delta = Double(5 - quality)
        let updated = easeFactor + (0.1 - delta * (0.08 + delta * 0.02))

        return max(ReviewState.minimumEaseFactor, updated)
    }
}
