import Foundation
import Testing

@testable import WordingKit

@Suite struct StarterPackTests {
    static let teraz = Date(timeIntervalSince1970: 1_786_000_000)

    @Test func pakietStartowyJestDolaczonyDoPakietu() throws {
        let pakiet = try StarterPack.load()

        #expect(pakiet.count == 38)
        #expect(pakiet.contains { $0.original == "scope" && $0.translation == "zakres" })
    }

    @Test func pakietStartowyNieMaPustychWpisow() throws {
        let pakiet = try StarterPack.load()

        for wpis in pakiet {
            #expect(!wpis.original.isEmpty)
            #expect(!wpis.translation.isEmpty)
        }
    }

    @Test func pakietStartowyZachowujePolskieZnaki() throws {
        let pakiet = try StarterPack.load()

        #expect(pakiet.contains { $0.translation.contains("domyślnie") })
    }

    @Test func zasiewaPustyMagazyn() throws {
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)

        let dodane = try magazyn.seedIfEmpty(now: Self.teraz)

        #expect(dodane == 38)
        #expect(magazyn.words.count == 38)

        // Wszystkie od razu wymagalne i jeszcze nieoceniane.
        for slowo in magazyn.words {
            #expect(slowo.review.dueUtc == Self.teraz)
            #expect(slowo.review.lastReviewedUtc == nil)
        }
    }

    @Test func zasianeSlowkaMajaUnikalneIdentyfikatory() throws {
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)

        try magazyn.seedIfEmpty(now: Self.teraz)

        #expect(Set(magazyn.words.map(\.id)).count == 38)
    }

    @Test func nieDotykaMagazynuKtoryJuzCosZawiera() throws {
        // Krytyczne: plik moze pochodzic z powloki .NET i zawierac stan powtorek.
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)
        _ = try magazyn.add(original: "juz-tu-bylo", translation: "istniejace", now: Self.teraz)

        let dodane = try magazyn.seedIfEmpty(now: Self.teraz)

        #expect(dodane == 0)
        #expect(magazyn.words.count == 1)
        #expect(magazyn.words[0].original == "juz-tu-bylo")
    }

    @Test func zasianyMagazynDaSieOdczytacPonownieZDysku() throws {
        let katalog = try TempKatalog()
        try WordStore(fileURL: katalog.plik).seedIfEmpty(now: Self.teraz)

        let poWczytaniu = try WordStore(fileURL: katalog.plik)

        #expect(poWczytaniu.words.count == 38)
    }
}
