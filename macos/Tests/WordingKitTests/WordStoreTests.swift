import Foundation
import Testing

@testable import WordingKit

@Suite struct WordStoreTests {
    static let teraz = Fixtures.teraz

    @Test func brakPlikuDajePustyMagazyn() throws {
        let dir = try TempDirectory()

        #expect(try WordStore(fileURL: dir.jsonFile).words.isEmpty)
    }

    @Test func dodanieZapisujeNaDysk() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.teraz)

        let reloaded = try WordStore(fileURL: dir.jsonFile)

        #expect(reloaded.words.count == 1)
        #expect(reloaded.words[0].original == "scope")
        #expect(reloaded.words[0].translation == "zakres")
    }

    @Test func dodanieNadajeUnikalneIdentyfikatory() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        for i in 0..<50 {
            try store.add(original: "slowo\(i)", translation: "tlum\(i)", now: Self.teraz)
        }

        #expect(Set(store.words.map(\.id)).count == 50)
    }

    @Test func dodaneSlowkoJestNoweIWymagalne() throws {
        let dir = try TempDirectory()

        let word = try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.teraz)

        #expect(word.isNew)
        #expect(word.isDue(at: Self.teraz))
    }

    @Test func usuniecieDzialaIZwracaFalseDlaNieistniejacego() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        let word = try store.add(original: "scope", translation: "zakres", now: Self.teraz)

        #expect(try store.remove(id: word.id) == true)
        #expect(try store.remove(id: word.id) == false)
        #expect(try WordStore(fileURL: dir.jsonFile).words.isEmpty)
    }

    @Test func aktualizacjaUtrwalaStanPowtorek() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        var word = try store.add(original: "scope", translation: "zakres", now: Self.teraz)

        word.review = SpacedRepetitionScheduler.apply(word.review, grade: .good, now: Self.teraz)
        #expect(try store.update(word) == true)

        let fromDisk = try WordStore(fileURL: dir.jsonFile).word(id: word.id)

        #expect(fromDisk?.review.repetitions == 1)
        #expect(fromDisk?.review.dueUtc == Self.teraz.addingTimeInterval(.day))
    }

    @Test func zapisNieZostawiaPlikuTymczasowego() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.teraz)

        let files = try FileManager.default.contentsOfDirectory(atPath: dir.path.path)

        #expect(!files.contains { $0.hasSuffix(".tmp") })
    }

    @Test func managerOdrzucaPusteWartosci() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        #expect(throws: WordingError.emptyOriginal) {
            try manager.addWord(original: "   ", translation: "zakres")
        }
        #expect(throws: WordingError.emptyTranslation) {
            try manager.addWord(original: "scope", translation: "")
        }
    }

    @Test func managerPrzycinaBialeZnaki() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        let word = try manager.addWord(original: "  scope  ", translation: "\tzakres\n")

        #expect(word.original == "scope")
        #expect(word.translation == "zakres")
    }

    @Test func ocenaPrzeliczaTerminIZapisujeGoNaDysk() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))
        let word = try manager.addWord(original: "scope", translation: "zakres", now: Self.teraz)

        #expect(try manager.grade(id: word.id, grade: .good, now: Self.teraz) == true)

        let fromDisk = try WordStore(fileURL: dir.jsonFile).word(id: word.id)
        #expect(fromDisk?.review.repetitions == 1)
    }

    @Test func ocenaNieistniejacegoSlowkaZwracaFalse() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        #expect(try manager.grade(id: UUID(), grade: .good) == false)
    }
}
