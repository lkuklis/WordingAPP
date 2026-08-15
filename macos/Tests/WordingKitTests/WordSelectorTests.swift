import Foundation
import Testing

@testable import WordingKit

@Suite struct WordSelectorTests {
    static let teraz = Fixtures.teraz

    static func slowko(
        _ original: String,
        due: Date? = nil,
        reviewed: Bool = true
    ) -> Word {
        Word(
            original: original,
            translation: original + "-pl",
            createdUtc: teraz,
            review: ReviewState(
                repetitions: reviewed ? 1 : 0,
                dueUtc: due ?? teraz,
                lastReviewedUtc: reviewed ? teraz.addingTimeInterval(-.day) : nil
            )
        )
    }

    @Test func pustaListaZwracaNil() {
        var generator = SeededGenerator(seed: 1)

        #expect(WordSelector.pickNext(from: [], now: Self.teraz, using: &generator) == nil)
    }

    @Test func jednoSlowkoZwracaJe() {
        var generator = SeededGenerator(seed: 1)
        let word = Self.slowko("scope")

        #expect(WordSelector.pickNext(from: [word], now: Self.teraz, using: &generator)?.id == word.id)
    }

    @Test func noweSlowkoMaWyzszaWageNizSlowkoWTerminie() {
        let fresh = Self.slowko("nowe", reviewed: false)
        let due = Self.slowko("wterminie", due: Self.teraz)

        #expect(
            WordSelector.weight(for: fresh, now: Self.teraz)
                > WordSelector.weight(for: due, now: Self.teraz)
        )
    }

    @Test func wagaRosnieWrazZOpoznieniem() {
        let onTime = WordSelector.weight(for: Self.slowko("swieze", due: Self.teraz), now: Self.teraz)
        let oneDay = WordSelector.weight(
            for: Self.slowko("dzien", due: Self.teraz.addingTimeInterval(-.day)), now: Self.teraz)
        let oneWeek = WordSelector.weight(
            for: Self.slowko("tydzien", due: Self.teraz.addingTimeInterval(-7 * .day)), now: Self.teraz)

        #expect(onTime < oneDay)
        #expect(oneDay < oneWeek)
    }

    @Test func wagaJestOgraniczonaZGory() {
        let year = WordSelector.weight(
            for: Self.slowko("stare", due: Self.teraz.addingTimeInterval(-365 * .day)), now: Self.teraz)
        let decade = WordSelector.weight(
            for: Self.slowko("bardzostare", due: Self.teraz.addingTimeInterval(-3650 * .day)), now: Self.teraz)

        #expect(year == decade)
    }

    @Test func slowkoNiewymagalneMaMalaAleNiezerowaWage() {
        let weight = WordSelector.weight(
            for: Self.slowko("znane", due: Self.teraz.addingTimeInterval(30 * .day)), now: Self.teraz)

        #expect(weight > 0)
        #expect(weight < WordSelector.dueWeight)
    }

    @Test func przeterminowaneJestLosowaneZnacznieCzesciej() {
        let overdue = Self.slowko("zapomniane", due: Self.teraz.addingTimeInterval(-10 * .day))
        let known = Self.slowko("znane", due: Self.teraz.addingTimeInterval(30 * .day))
        let words = [overdue, known]

        var generator = SeededGenerator(seed: 42)
        var hits = 0
        let attempts = 2000

        for _ in 0..<attempts {
            if WordSelector.pickNext(from: words, now: Self.teraz, using: &generator)?.id == overdue.id {
                hits += 1
            }
        }

        #expect(hits > Int(Double(attempts) * 0.9))
    }

    @Test func przyRownychWagachRozkladJestZblizonyDoJednostajnego() {
        let words = [Self.slowko("a"), Self.slowko("b"), Self.slowko("c")]
        var generator = SeededGenerator(seed: 7)
        var counts: [String: Int] = ["a": 0, "b": 0, "c": 0]

        for _ in 0..<3000 {
            let picked = WordSelector.pickNext(from: words, now: Self.teraz, using: &generator)!
            counts[picked.original, default: 0] += 1
        }

        for (word, count) in counts {
            #expect(count > 800 && count < 1200, "\(word) trafilo \(count) razy")
        }
    }
}
