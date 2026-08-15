import Foundation

/// Algorytm SM-2 (SuperMemo 2) jako czysta funkcja.
///
/// Port `Wording.Core.Learning.SpacedRepetitionScheduler` - obie implementacje
/// musza dawac identyczne liczby, bo pracuja na tym samym pliku danych.
public enum SpacedRepetitionScheduler {
    /// Odstep po pierwszej udanej powtorce.
    static let firstIntervalDays = 1.0

    /// Odstep po drugiej udanej powtorce.
    static let secondIntervalDays = 6.0

    /// Po ocenie `again` slowko wraca po chwili, a nie natychmiast - inaczej
    /// zablokowaloby cala rotacje, bo byloby stale najbardziej przeterminowane.
    static let relearnDelay: TimeInterval = 10 * 60

    public static func apply(_ current: ReviewState, grade: ReviewGrade, now: Date) -> ReviewState {
        let quality = grade.rawValue
        let easeFactor = nextEaseFactor(current.easeFactor, quality: quality)

        // SM-2 traktuje ocene ponizej 3 jako nietrafiona.
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

    /// Oryginalny wzor SM-2: EF' = EF + (0.1 - (5-q) * (0.08 + (5-q) * 0.02)).
    static func nextEaseFactor(_ easeFactor: Double, quality: Int) -> Double {
        let delta = Double(5 - quality)
        let updated = easeFactor + (0.1 - delta * (0.08 + delta * 0.02))

        return max(ReviewState.minimumEaseFactor, updated)
    }
}
