import Foundation
import Testing

@testable import WordingKit

@Suite struct WordSetCatalogTests {
    static let source = URL(string: "https://example.com/pack.json")!

    @discardableResult
    static func `import`(
        _ dir: TempDirectory,
        id: String,
        name: String,
        _ words: (String, String)...
    ) throws -> PackImportResult {
        try WordPackImporter(setsDirectory: dir.setsDirectory).import(
            WordPack(
                id: id,
                name: name,
                words: words.map { PackEntry(original: $0.0, translation: $0.1) }
            ),
            from: source,
            now: Fixtures.now
        )
    }

    @Test func resolveActiveFileWithNoChoiceOpensTheUsersOwnWords() throws {
        let dir = try TempDirectory()

        #expect(
            WordSetCatalog.resolveActiveFile(nil, dataFile: dir.jsonFile, setsDirectory: dir.setsDirectory)
                == dir.jsonFile)
        #expect(
            WordSetCatalog.resolveActiveFile("", dataFile: dir.jsonFile, setsDirectory: dir.setsDirectory)
                == dir.jsonFile)
    }

    @Test func resolveActiveFileOpensTheChosenSet() throws {
        let dir = try TempDirectory()
        try Self.import(dir, id: "travel-basics", name: "Travel basics", ("airport", "aeropuerto"))

        #expect(
            WordSetCatalog.resolveActiveFile(
                "travel-basics", dataFile: dir.jsonFile, setsDirectory: dir.setsDirectory)
                == dir.setFile("travel-basics"))
    }

    @Test func resolveActiveFileFallsBackWhenTheRememberedSetIsGone() throws {
        // Deleted by hand between runs. Refusing to start would leave the user with an
        // app that will not open.
        let dir = try TempDirectory()

        #expect(
            WordSetCatalog.resolveActiveFile(
                "deleted-set", dataFile: dir.jsonFile, setsDirectory: dir.setsDirectory)
                == dir.jsonFile)
    }

    @Test(arguments: ["../words", "/etc/passwd", "con"])
    func resolveActiveFileNeverHonoursAnIdentifierThatIsNotASafeSlug(setId: String) throws {
        // The remembered id comes out of UserDefaults, so it gets the same treatment as
        // one from a downloaded pack.
        let dir = try TempDirectory()

        #expect(
            WordSetCatalog.resolveActiveFile(setId, dataFile: dir.jsonFile, setsDirectory: dir.setsDirectory)
                == dir.jsonFile)
    }

    @Test func isEmptyBeforeAnythingIsImported() throws {
        let dir = try TempDirectory()

        #expect(WordSetCatalog.list(in: dir.setsDirectory).isEmpty)
    }

    @Test func reportsTheNameFromTheHeaderAndTheCountFromTheWords() throws {
        let dir = try TempDirectory()
        try Self.import(
            dir, id: "travel-basics", name: "Travel basics",
            ("airport", "aeropuerto"), ("ticket", "billete"))

        let set = try #require(WordSetCatalog.list(in: dir.setsDirectory).first)

        #expect(set.id == "travel-basics")
        #expect(set.name == "Travel basics")
        #expect(set.wordCount == 2)
        #expect(set.sourceUrl == Self.source.absoluteString)
    }

    @Test func countsWhatIsInTheFileRatherThanWhatWasImported() throws {
        // A stored count would start lying the moment a word is deleted.
        let dir = try TempDirectory()
        try Self.import(
            dir, id: "travel-basics", name: "Travel basics",
            ("airport", "aeropuerto"), ("ticket", "billete"))

        let store = try WordStore(fileURL: dir.setFile("travel-basics"))
        try store.remove(id: try #require(store.words.first?.id))

        #expect(WordSetCatalog.list(in: dir.setsDirectory).first?.wordCount == 1)
    }

    @Test func skipsAFileItCannotUnderstandInsteadOfFailing() throws {
        let dir = try TempDirectory()
        try Self.import(dir, id: "good", name: "Good one", ("airport", "aeropuerto"))
        try Data("{ not json".utf8).write(to: dir.setFile("broken"))

        let sets = WordSetCatalog.list(in: dir.setsDirectory)

        #expect(sets.count == 1)
        #expect(sets[0].id == "good")
    }

    @Test func takesTheIdentifierFromTheFileNameNotTheHeader() throws {
        // They disagree once a file is renamed by hand, and the name on disk is the one
        // that decides which file a refresh would touch.
        let dir = try TempDirectory()
        try Self.import(dir, id: "travel-basics", name: "Travel basics", ("airport", "aeropuerto"))

        try FileManager.default.moveItem(at: dir.setFile("travel-basics"), to: dir.setFile("renamed"))

        #expect(WordSetCatalog.list(in: dir.setsDirectory).first?.id == "renamed")
    }

    @Test func ignoresTheUsersOwnWordsFile() throws {
        // words.json is not an import and lives outside the sets directory.
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile).add(original: "mine", translation: "moje", now: Fixtures.now)
        try Self.import(dir, id: "travel-basics", name: "Travel basics", ("airport", "aeropuerto"))

        let sets = WordSetCatalog.list(in: dir.setsDirectory)

        #expect(sets.count == 1)
        #expect(sets[0].id == "travel-basics")
    }
}
