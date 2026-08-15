import Foundation
import Testing

@testable import WordingKit

@Suite struct WordSelectorTests {
    static let now = Fixtures.now

    static func word(_ original: String, due: Date? = nil, reviewed: Bool = true) -> Word {
        Word(
            original: original,
            translation: original + "-translated",
            createdUtc: now,
            review: ReviewState(
                repetitions: reviewed ? 1 : 0,
                dueUtc: due ?? now,
                lastReviewedUtc: reviewed ? now.addingTimeInterval(-.day) : nil
            )
        )
    }

    @Test func emptyListReturnsNil() {
        var generator = SeededGenerator(seed: 1)

        #expect(WordSelector.pickNext(from: [], now: Self.now, using: &generator) == nil)
    }

    @Test func singleWordReturnsThatWord() {
        var generator = SeededGenerator(seed: 1)
        let word = Self.word("scope")

        #expect(WordSelector.pickNext(from: [word], now: Self.now, using: &generator)?.id == word.id)
    }

    @Test func newWordOutweighsAWordThatIsExactlyDue() {
        let fresh = Self.word("new", reviewed: false)
        let due = Self.word("due", due: Self.now)

        #expect(
            WordSelector.weight(for: fresh, now: Self.now)
                > WordSelector.weight(for: due, now: Self.now)
        )
    }

    @Test func weightGrowsWithLateness() {
        let onTime = WordSelector.weight(for: Self.word("on-time", due: Self.now), now: Self.now)
        let oneDay = WordSelector.weight(
            for: Self.word("one-day", due: Self.now.addingTimeInterval(-.day)), now: Self.now)
        let oneWeek = WordSelector.weight(
            for: Self.word("one-week", due: Self.now.addingTimeInterval(-7 * .day)), now: Self.now)

        #expect(onTime < oneDay)
        #expect(oneDay < oneWeek)
    }

    @Test func weightIsCapped() {
        let year = WordSelector.weight(
            for: Self.word("old", due: Self.now.addingTimeInterval(-365 * .day)), now: Self.now)
        let decade = WordSelector.weight(
            for: Self.word("ancient", due: Self.now.addingTimeInterval(-3650 * .day)), now: Self.now)

        #expect(year == decade)
    }

    @Test func wordNotYetDueKeepsASmallButNonZeroWeight() {
        let weight = WordSelector.weight(
            for: Self.word("known", due: Self.now.addingTimeInterval(30 * .day)), now: Self.now)

        #expect(weight > 0)
        #expect(weight < WordSelector.dueWeight)
    }

    @Test func overdueWordIsDrawnFarMoreOften() {
        let overdue = Self.word("forgotten", due: Self.now.addingTimeInterval(-10 * .day))
        let known = Self.word("known", due: Self.now.addingTimeInterval(30 * .day))
        let words = [overdue, known]

        var generator = SeededGenerator(seed: 42)
        var hits = 0
        let attempts = 2000

        for _ in 0..<attempts {
            if WordSelector.pickNext(from: words, now: Self.now, using: &generator)?.id == overdue.id {
                hits += 1
            }
        }

        #expect(hits > Int(Double(attempts) * 0.9))
    }

    @Test func withEqualWeightsTheDistributionIsRoughlyUniform() {
        let words = [Self.word("a"), Self.word("b"), Self.word("c")]
        var generator = SeededGenerator(seed: 7)
        var counts: [String: Int] = ["a": 0, "b": 0, "c": 0]

        for _ in 0..<3000 {
            let picked = WordSelector.pickNext(from: words, now: Self.now, using: &generator)!
            counts[picked.original, default: 0] += 1
        }

        for (word, count) in counts {
            #expect(count > 800 && count < 1200, "\(word) was picked \(count) times")
        }
    }
}
