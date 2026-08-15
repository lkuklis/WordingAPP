import Foundation

/// Picks the next word to show.
///
/// A port of `Wording.Core.Learning.WordSelector`. It deliberately does NOT gate on the
/// due date the way a conventional SRS would: this app shows a word every few minutes in
/// the background rather than in review sessions, so due-date gating would leave it with
/// nothing to display most of the time. Instead every word gets a weight.
public enum WordSelector {
    /// A word that has never been graded should show often.
    static let newWordWeight = 10.0

    /// Weight of a word exactly at its due date. Each day of delay adds 1.
    static let dueWeight = 1.0

    /// Cap on lateness, so one word forgotten a year ago cannot dominate the rotation.
    static let maxOverdueDays = 30.0

    /// Floor - small, but non-zero, so nothing drops out of rotation.
    static let minWeight = 0.02

    /// Returns a word to show, or nil when the list is empty.
    public static func pickNext(
        from words: [Word],
        now: Date,
        using generator: inout some RandomNumberGenerator
    ) -> Word? {
        guard !words.isEmpty else { return nil }

        let weights = words.map { weight(for: $0, now: now) }
        let total = weights.reduce(0, +)

        var roll = Double.random(in: 0..<total, using: &generator)

        for (index, weight) in weights.enumerated() {
            roll -= weight
            if roll <= 0 {
                return words[index]
            }
        }

        // Unreachable except for rounding error in the floating-point sum.
        return words.last
    }

    public static func pickNext(from words: [Word], now: Date) -> Word? {
        var generator = SystemRandomNumberGenerator()
        return pickNext(from: words, now: now, using: &generator)
    }

    /// Weight of a word: the more urgent it is, the likelier it is to be drawn.
    static func weight(for word: Word, now: Date) -> Double {
        guard !word.isNew else { return newWordWeight }

        let overdueDays = now.timeIntervalSince(word.review.dueUtc) / .day

        if overdueDays >= 0 {
            return dueWeight + min(overdueDays, maxOverdueDays)
        }

        // Not due yet - the weight shrinks the further away the due date is.
        return max(minWeight, dueWeight / (1 - overdueDays))
    }
}
