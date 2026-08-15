import Foundation
import Testing

@testable import WordingKit

/// Izolowany katalog na dane, sprzatany po tescie.
final class TempKatalog {
    let katalog: URL

    init() throws {
        katalog = URL.temporaryDirectory.appending(path: "wording-test-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: katalog, withIntermediateDirectories: true)
    }

    var plik: URL { katalog.appending(path: "words.json") }

    deinit {
        try? FileManager.default.removeItem(at: katalog)
    }
}

@Suite struct WordStoreTests {
    static let teraz = Date(timeIntervalSince1970: 1_786_000_000)

    @Test func brakPlikuDajePustyMagazyn() throws {
        let katalog = try TempKatalog()

        let magazyn = try WordStore(fileURL: katalog.plik)

        #expect(magazyn.words.isEmpty)
    }

    @Test func dodanieZapisujeNaDysk() throws {
        let katalog = try TempKatalog()
        _ = try WordStore(fileURL: katalog.plik)
            .add(original: "scope", translation: "zakres", now: Self.teraz)

        let poWczytaniu = try WordStore(fileURL: katalog.plik)

        #expect(poWczytaniu.words.count == 1)
        #expect(poWczytaniu.words[0].original == "scope")
        #expect(poWczytaniu.words[0].translation == "zakres")
    }

    @Test func dodanieNadajeUnikalneIdentyfikatory() throws {
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)

        for i in 0..<50 {
            _ = try magazyn.add(original: "slowo\(i)", translation: "tlum\(i)", now: Self.teraz)
        }

        #expect(Set(magazyn.words.map(\.id)).count == 50)
    }

    @Test func usuniecieDzialaIZwracaFalseDlaNieistniejacego() throws {
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)
        let slowo = try magazyn.add(original: "scope", translation: "zakres", now: Self.teraz)

        #expect(try magazyn.remove(id: slowo.id) == true)
        #expect(try magazyn.remove(id: slowo.id) == false)
        #expect(try WordStore(fileURL: katalog.plik).words.isEmpty)
    }

    @Test func aktualizacjaUtrwalaStanPowtorek() throws {
        let katalog = try TempKatalog()
        let magazyn = try WordStore(fileURL: katalog.plik)
        var slowo = try magazyn.add(original: "scope", translation: "zakres", now: Self.teraz)

        slowo.review = SpacedRepetitionScheduler.apply(slowo.review, grade: .good, now: Self.teraz)
        #expect(try magazyn.update(slowo) == true)

        let zDysku = try WordStore(fileURL: katalog.plik).word(id: slowo.id)

        #expect(zDysku?.review.repetitions == 1)
        #expect(zDysku?.review.dueUtc == Self.teraz.addingTimeInterval(86_400))
    }

    @Test func zapisNieZostawiaPlikuTymczasowego() throws {
        let katalog = try TempKatalog()
        _ = try WordStore(fileURL: katalog.plik)
            .add(original: "scope", translation: "zakres", now: Self.teraz)

        let pliki = try FileManager.default.contentsOfDirectory(atPath: katalog.katalog.path)

        #expect(!pliki.contains { $0.hasSuffix(".tmp") })
    }

    @Test func managerOdrzucaPusteWartosci() throws {
        let katalog = try TempKatalog()
        let manager = WordManager(store: try WordStore(fileURL: katalog.plik))

        #expect(throws: WordingError.emptyOriginal) {
            try manager.addWord(original: "   ", translation: "zakres")
        }
        #expect(throws: WordingError.emptyTranslation) {
            try manager.addWord(original: "scope", translation: "")
        }
    }

    @Test func managerPrzycinaBialeZnaki() throws {
        let katalog = try TempKatalog()
        let manager = WordManager(store: try WordStore(fileURL: katalog.plik))

        let slowo = try manager.addWord(original: "  scope  ", translation: "\tzakres\n")

        #expect(slowo.original == "scope")
        #expect(slowo.translation == "zakres")
    }

    @Test func ocenaPrzeliczaTerminIZapisujeGoNaDysk() throws {
        let katalog = try TempKatalog()
        let manager = WordManager(store: try WordStore(fileURL: katalog.plik))
        let slowo = try manager.addWord(original: "scope", translation: "zakres", now: Self.teraz)

        #expect(try manager.grade(id: slowo.id, grade: .good, now: Self.teraz) == true)

        let zDysku = try WordStore(fileURL: katalog.plik).word(id: slowo.id)
        #expect(zDysku?.review.repetitions == 1)
    }

    @Test func ocenaNieistniejacegoSlowkaZwracaFalse() throws {
        let katalog = try TempKatalog()
        let manager = WordManager(store: try WordStore(fileURL: katalog.plik))

        #expect(try manager.grade(id: UUID(), grade: .good) == false)
    }
}
