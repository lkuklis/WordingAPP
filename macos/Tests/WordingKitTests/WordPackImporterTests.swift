import Foundation
import Testing

@testable import WordingKit

@Suite struct WordPackImporterTests {
    static let source = URL(string: "https://example.com/travel-basics.json")!
    static let now = Fixtures.now

    static func pack(_ words: (String, String)..., id: String = "travel-basics") -> WordPack {
        WordPack(
            id: id,
            name: "Travel basics",
            words: words.map { PackEntry(original: $0.0, translation: $0.1) }
        )
    }

    @Test func writesTheSetToItsOwnFile() throws {
        let dir = try TempDirectory()

        let result = try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        #expect(FileManager.default.fileExists(atPath: dir.setFile("travel-basics").path(percentEncoded: false)))
        #expect(result.set.id == "travel-basics")
        #expect(result.set.name == "Travel basics")
        #expect(result.added == 1)
    }

    @Test func leavesTheUsersOwnWordsUntouched() throws {
        // The whole point of the feature: importing must not disturb what is open.
        let dir = try TempDirectory()
        let own = try WordStore(fileURL: dir.jsonFile)
        try own.add(original: "mine", translation: "moje", now: Self.now)

        let before = try Data(contentsOf: dir.jsonFile)

        try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        #expect(try Data(contentsOf: dir.jsonFile) == before)
        #expect(try WordStore(fileURL: dir.jsonFile).words.map(\.original) == ["mine"])
    }

    @Test func recordsTheHeaderSoTheSetCanBeRefreshedLater() throws {
        let dir = try TempDirectory()

        try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        let set = try #require(try WordStore(fileURL: dir.setFile("travel-basics")).set)

        #expect(set.id == "travel-basics")
        #expect(set.sourceUrl == Self.source.absoluteString)
        #expect(set.importedUtc == Self.now)
    }

    @Test func importedWordsStartNewAndDue() throws {
        let dir = try TempDirectory()

        try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        let word = try #require(try WordStore(fileURL: dir.setFile("travel-basics")).words.first)

        #expect(word.isNew)
        #expect(word.isDue(at: Self.now))
    }

    @Test func refusesToOverwriteASetAlreadyOnDisk() throws {
        let dir = try TempDirectory()
        let importer = WordPackImporter(setsDirectory: dir.setsDirectory)
        try importer.import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        #expect(throws: WordPackError.alreadyExists) {
            try importer.import(Self.pack(("other", "inne")), from: Self.source, now: Self.now)
        }

        // And the refused import changed nothing.
        #expect(try WordStore(fileURL: dir.setFile("travel-basics")).words.map(\.original) == ["airport"])
    }

    @Test func replacingMergesInsteadOfStartingOver() throws {
        let dir = try TempDirectory()
        let importer = WordPackImporter(setsDirectory: dir.setsDirectory)
        try importer.import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        let result = try importer.import(
            Self.pack(("airport", "aeropuerto"), ("ticket", "billete")),
            from: Self.source,
            replaceExisting: true,
            now: Self.now
        )

        #expect(result.added == 1)
        #expect(result.skipped == 1)
        #expect(try WordStore(fileURL: dir.setFile("travel-basics")).words.count == 2)
    }

    @Test func replacingKeepsTheReviewProgressOfWordsAlreadyThere() throws {
        // The one thing an import must never do is undo someone's learning.
        let dir = try TempDirectory()
        let importer = WordPackImporter(setsDirectory: dir.setsDirectory)
        try importer.import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        let store = try WordStore(fileURL: dir.setFile("travel-basics"))
        let manager = WordManager(store: store)
        let id = try #require(store.words.first?.id)

        #expect(try manager.grade(id: id, grade: .good, now: Self.now))

        let graded = try #require(store.word(id: id))

        try importer.import(
            Self.pack(("airport", "aeropuerto"), ("ticket", "billete")),
            from: Self.source,
            replaceExisting: true,
            now: Self.now
        )

        let reloaded = try #require(try WordStore(fileURL: dir.setFile("travel-basics")).word(id: id))

        #expect(!reloaded.isNew)
        #expect(reloaded.review.repetitions == graded.review.repetitions)
        #expect(reloaded.review.dueUtc == graded.review.dueUtc)
    }

    @Test(arguments: [("Airport", "AEROPUERTO"), ("  airport  ", " aeropuerto ")])
    func treatsTheSameWordAsAlreadyPresentWhateverTheCaseOrSpacing(
        original: String,
        translation: String
    ) throws {
        let dir = try TempDirectory()
        let importer = WordPackImporter(setsDirectory: dir.setsDirectory)
        try importer.import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        let result = try importer.import(
            Self.pack((original, translation)),
            from: Self.source,
            replaceExisting: true,
            now: Self.now
        )

        #expect(result.added == 0)
        #expect(try WordStore(fileURL: dir.setFile("travel-basics")).words.count == 1)
    }

    @Test func countsAWordRepeatedInsideThePackOnlyOnce() throws {
        let dir = try TempDirectory()

        let result = try WordPackImporter(setsDirectory: dir.setsDirectory).import(
            Self.pack(("airport", "aeropuerto"), ("airport", "aeropuerto")),
            from: Self.source,
            now: Self.now
        )

        #expect(result.added == 1)
    }

    @Test func refusesAnIdentifierThatWouldEscapeTheSetsDirectory() throws {
        let dir = try TempDirectory()

        #expect(throws: WordPackError.unsafeId) {
            try WordPackImporter(setsDirectory: dir.setsDirectory).import(
                Self.pack(("airport", "aeropuerto"), id: "../words"),
                from: Self.source,
                now: Self.now
            )
        }

        #expect(!FileManager.default.fileExists(atPath: dir.jsonFile.path(percentEncoded: false)))
    }

    @Test func carriesTheKindIntoTheSetHeader() throws {
        // The UI reads it from the header, not from the pack, which is long gone by then.
        let dir = try TempDirectory()
        var pack = Self.pack(("idempotency", "Doing it twice changes nothing more."))
        pack.kind = PackKind.concepts

        let result = try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(pack, from: Self.source, now: Self.now)

        #expect(try WordStore(fileURL: dir.setFile("travel-basics")).set?.kind == PackKind.concepts)
        #expect(result.set.kind == PackKind.concepts)
    }

    @Test func withoutAKindRecordsVocabulary() throws {
        let dir = try TempDirectory()

        let result = try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        #expect(result.set.kind == PackKind.vocabulary)
    }

    @Test func setExistsAnswersByIdentifierBeforeAnythingIsDownloaded() throws {
        // A catalogue row knows only the identifier, so this is what marks it Installed.
        let dir = try TempDirectory()
        let importer = WordPackImporter(setsDirectory: dir.setsDirectory)

        #expect(!importer.setExists("travel-basics"))

        try importer.import(Self.pack(("airport", "aeropuerto")), from: Self.source, now: Self.now)

        #expect(importer.setExists("travel-basics"))
        #expect(importer.setExists("Travel-Basics"))
        #expect(!importer.setExists("../words"))
        #expect(!importer.setExists("never-imported"))
    }

    @Test func withoutASourceUrlStillWorks() throws {
        // A pack opened from a local file has no address to record.
        let dir = try TempDirectory()

        try WordPackImporter(setsDirectory: dir.setsDirectory)
            .import(Self.pack(("airport", "aeropuerto")), from: nil, now: Self.now)

        #expect(try WordStore(fileURL: dir.setFile("travel-basics")).set?.sourceUrl == nil)
    }
}
