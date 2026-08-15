import Foundation
import Testing

@testable import WordingKit

/// Deterministyczny generator, zeby testy rozkladu nie byly kruche.
struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64

    init(seed: UInt64) { state = seed &+ 0x9E37_79B9_7F4A_7C15 }

    mutating func next() -> UInt64 {
        state = state &+ 0x9E37_79B9_7F4A_7C15
        var z = state
        z = (z ^ (z >> 30)) &* 0xBF58_476D_1CE4_E5B9
        z = (z ^ (z >> 27)) &* 0x94D0_49BB_1331_11EB
        return z ^ (z >> 31)
    }
}

@Suite struct WordSelectorTests {
    static let teraz = Date(timeIntervalSince1970: 1_786_000_000)

    static func slowko(
        _ oryginal: String,
        termin: Date? = nil,
        juzPowtarzane: Bool = true
    ) -> Word {
        Word(
            original: oryginal,
            translation: oryginal + "-pl",
            createdUtc: teraz,
            review: ReviewState(
                repetitions: juzPowtarzane ? 1 : 0,
                dueUtc: termin ?? teraz,
                lastReviewedUtc: juzPowtarzane ? teraz.addingTimeInterval(-86_400) : nil
            )
        )
    }

    @Test func pustaListaZwracaNil() {
        var generator = SeededGenerator(seed: 1)

        #expect(WordSelector().pickNext(from: [], now: Self.teraz, using: &generator) == nil)
    }

    @Test func jednoSlowkoZwracaJe() {
        var generator = SeededGenerator(seed: 1)
        let slowo = Self.slowko("scope")

        let wybrane = WordSelector().pickNext(from: [slowo], now: Self.teraz, using: &generator)

        #expect(wybrane?.id == slowo.id)
    }

    @Test func noweSlowkoMaWyzszaWageNizSlowkoWTerminie() {
        let nowe = Self.slowko("nowe", juzPowtarzane: false)
        let wTerminie = Self.slowko("wterminie", termin: Self.teraz)

        #expect(
            WordSelector.weight(for: nowe, now: Self.teraz)
                > WordSelector.weight(for: wTerminie, now: Self.teraz)
        )
    }

    @Test func wagaRosnieWrazZOpoznieniem() {
        let swieze = Self.slowko("swieze", termin: Self.teraz)
        let dzien = Self.slowko("dzien", termin: Self.teraz.addingTimeInterval(-86_400))
        let tydzien = Self.slowko("tydzien", termin: Self.teraz.addingTimeInterval(-7 * 86_400))

        let w1 = WordSelector.weight(for: swieze, now: Self.teraz)
        let w2 = WordSelector.weight(for: dzien, now: Self.teraz)
        let w3 = WordSelector.weight(for: tydzien, now: Self.teraz)

        #expect(w1 < w2)
        #expect(w2 < w3)
    }

    @Test func wagaJestOgraniczonaZGory() {
        let rok = Self.slowko("stare", termin: Self.teraz.addingTimeInterval(-365 * 86_400))
        let dekada = Self.slowko("bardzostare", termin: Self.teraz.addingTimeInterval(-3650 * 86_400))

        #expect(
            WordSelector.weight(for: rok, now: Self.teraz)
                == WordSelector.weight(for: dekada, now: Self.teraz)
        )
    }

    @Test func slowkoNiewymagalneMaMalaAleNiezerowaWage() {
        let zaMiesiac = Self.slowko("znane", termin: Self.teraz.addingTimeInterval(30 * 86_400))

        let waga = WordSelector.weight(for: zaMiesiac, now: Self.teraz)

        #expect(waga > 0)
        #expect(waga < WordSelector.dueWeight)
    }

    @Test func przeterminowaneJestLosowaneZnacznieCzesciej() {
        let przeterminowane = Self.slowko("zapomniane", termin: Self.teraz.addingTimeInterval(-10 * 86_400))
        let znane = Self.slowko("znane", termin: Self.teraz.addingTimeInterval(30 * 86_400))
        let lista = [przeterminowane, znane]

        var generator = SeededGenerator(seed: 42)
        var trafienia = 0
        let prob = 2000

        for _ in 0..<prob {
            if WordSelector().pickNext(from: lista, now: Self.teraz, using: &generator)?.id
                == przeterminowane.id
            {
                trafienia += 1
            }
        }

        #expect(trafienia > Int(Double(prob) * 0.9))
    }

    @Test func przyRownychWagachRozkladJestZblizonyDoJednostajnego() {
        let lista = [Self.slowko("a"), Self.slowko("b"), Self.slowko("c")]
        var generator = SeededGenerator(seed: 7)
        var licznik: [String: Int] = ["a": 0, "b": 0, "c": 0]

        for _ in 0..<3000 {
            let wybrane = WordSelector().pickNext(from: lista, now: Self.teraz, using: &generator)!
            licznik[wybrane.original, default: 0] += 1
        }

        for (slowo, ile) in licznik {
            #expect(ile > 800 && ile < 1200, "\(slowo) trafilo \(ile) razy")
        }
    }
}
