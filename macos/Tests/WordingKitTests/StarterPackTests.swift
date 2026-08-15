import Foundation
import Testing

@testable import WordingKit

@Suite struct StarterPackTests {
    static let teraz = Fixtures.teraz

    @Test func pakietStartowyJestDolaczonyDoPakietu() throws {
        let pack = try StarterPack.load()

        #expect(pack.count == 38)
        #expect(pack.contains { $0.original == "scope" && $0.translation == "zakres" })
    }

    @Test func pakietStartowyNieMaPustychWpisow() throws {
        for entry in try StarterPack.load() {
            #expect(!entry.original.isEmpty)
            #expect(!entry.translation.isEmpty)
        }
    }

    @Test func pakietStartowyZachowujePolskieZnaki() throws {
        #expect(try StarterPack.load().contains { $0.translation.contains("domyślnie") })
    }

    @Test func zasiewaPustyMagazyn() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        #expect(try store.seedIfEmpty(now: Self.teraz) == 38)
        #expect(store.words.count == 38)

        // Wszystkie od razu wymagalne i jeszcze nieoceniane.
        for word in store.words {
            #expect(word.isNew)
            #expect(word.isDue(at: Self.teraz))
        }
    }

    @Test func zasianeSlowkaMajaUnikalneIdentyfikatory() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        try store.seedIfEmpty(now: Self.teraz)

        #expect(Set(store.words.map(\.id)).count == 38)
    }

    @Test func nieDotykaMagazynuKtoryJuzCosZawiera() throws {
        // Krytyczne: plik moze pochodzic z aplikacji .NET i zawierac stan powtorek.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        try store.add(original: "juz-tu-bylo", translation: "istniejace", now: Self.teraz)

        #expect(try store.seedIfEmpty(now: Self.teraz) == 0)
        #expect(store.words.count == 1)
        #expect(store.words[0].original == "juz-tu-bylo")
    }

    @Test func zasianyMagazynDaSieOdczytacPonownieZDysku() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile).seedIfEmpty(now: Self.teraz)

        #expect(try WordStore(fileURL: dir.jsonFile).words.count == 38)
    }

    @Test func zasiewRobiJedenZapisNieJedenNaSlowko() throws {
        // Wczesniej kazde slowko szlo osobnym add(), czyli 38 pelnych zapisow pliku.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        try store.seedIfEmpty(now: Self.teraz)

        // Wszystkie slowka maja ten sam znacznik czasu - slad jednego przebiegu.
        #expect(Set(store.words.map(\.createdUtc)).count == 1)
    }
}
