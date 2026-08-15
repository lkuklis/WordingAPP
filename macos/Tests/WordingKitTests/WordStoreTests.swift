import Foundation
import Testing

@testable import WordingKit

@Suite struct WordStoreRemoveAllTests {
    @Test func emptiesTheStoreAndKeepsACopy() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        try store.add(original: "scope", translation: "zakres", now: Fixtures.now)
        try store.add(original: "cater", translation: "zaspokoić", now: Fixtures.now)

        let backup = try #require(try store.removeAll(now: Fixtures.now))

        #expect(store.words.isEmpty)
        #expect(try WordStore(fileURL: dir.jsonFile).words.isEmpty)

        // The copy is only worth taking if it still holds what was deleted.
        let saved = try WordStore(fileURL: backup).words
        #expect(saved.count == 2)
        #expect(saved.contains { $0.original == "scope" })
    }

    @Test func onAnEmptyStoreDoesNothingAndBacksUpNothing() throws {
        // Otherwise clearing twice would replace the useful backup with a copy of nothing.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        #expect(try store.removeAll(now: Fixtures.now) == nil)

        let backups = dir.path.appending(path: WordingPaths.backupsFolderName)
        #expect(!FileManager.default.fileExists(atPath: backups.path(percentEncoded: false)))
    }

    @Test func putsTheBackupWhereTheSetCatalogueWillNotSeeIt() throws {
        // A backup written beside a set would otherwise be listed as a set of its own.
        let dir = try TempDirectory()
        try FileManager.default.createDirectory(at: dir.setsDirectory, withIntermediateDirectories: true)

        let store = try WordStore(fileURL: dir.setFile("travel-basics"))
        try store.describe(WordSet(id: "travel-basics", name: "Travel basics", importedUtc: Fixtures.now))
        try store.add(original: "airport", translation: "aeropuerto", now: Fixtures.now)

        #expect(try store.removeAll(now: Fixtures.now) != nil)

        let listed = WordSetCatalog.list(in: dir.setsDirectory)
        #expect(listed.count == 1)
        #expect(listed[0].id == "travel-basics")
    }

    @Test func keepsTheSetHeader() throws {
        // Emptying a set does not stop it being that set - the name and source stay.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        try store.describe(WordSet(id: "travel-basics", name: "Travel basics", importedUtc: Fixtures.now))
        try store.add(original: "airport", translation: "aeropuerto", now: Fixtures.now)

        try store.removeAll(now: Fixtures.now)

        #expect(try WordStore(fileURL: dir.jsonFile).set?.name == "Travel basics")
    }
}

@Suite struct WordStoreTests {
    static let now = Fixtures.now

    @Test func missingFileGivesAnEmptyStore() throws {
        let dir = try TempDirectory()

        #expect(try WordStore(fileURL: dir.jsonFile).words.isEmpty)
    }

    @Test func addWritesToDisk() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.now)

        let reloaded = try WordStore(fileURL: dir.jsonFile)

        #expect(reloaded.words.count == 1)
        #expect(reloaded.words[0].original == "scope")
        #expect(reloaded.words[0].translation == "zakres")
    }

    @Test func addAssignsUniqueIdentifiers() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        for i in 0..<50 {
            try store.add(original: "word\(i)", translation: "translation\(i)", now: Self.now)
        }

        #expect(Set(store.words.map(\.id)).count == 50)
    }

    @Test func anAddedWordIsNewAndDue() throws {
        let dir = try TempDirectory()

        let word = try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.now)

        #expect(word.isNew)
        #expect(word.isDue(at: Self.now))
    }

    @Test func removeWorksAndReturnsFalseForAnUnknownId() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        let word = try store.add(original: "scope", translation: "zakres", now: Self.now)

        #expect(try store.remove(id: word.id) == true)
        #expect(try store.remove(id: word.id) == false)
        #expect(try WordStore(fileURL: dir.jsonFile).words.isEmpty)
    }

    @Test func updatePersistsReviewState() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        var word = try store.add(original: "scope", translation: "zakres", now: Self.now)

        word.review = SpacedRepetitionScheduler.apply(word.review, grade: .good, now: Self.now)
        #expect(try store.update(word) == true)

        let fromDisk = try WordStore(fileURL: dir.jsonFile).word(id: word.id)

        #expect(fromDisk?.review.repetitions == 1)
        #expect(fromDisk?.review.dueUtc == Self.now.addingTimeInterval(.day))
    }

    @Test func saveLeavesNoTemporaryFileBehind() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile)
            .add(original: "scope", translation: "zakres", now: Self.now)

        let files = try FileManager.default.contentsOfDirectory(atPath: dir.path.path)

        #expect(!files.contains { $0.hasSuffix(".tmp") })
    }

    @Test func managerRejectsEmptyValues() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        #expect(throws: WordingError.emptyOriginal) {
            try manager.addWord(original: "   ", translation: "zakres")
        }
        #expect(throws: WordingError.emptyTranslation) {
            try manager.addWord(original: "scope", translation: "")
        }
    }

    @Test func managerTrimsWhitespace() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        let word = try manager.addWord(original: "  scope  ", translation: "\tzakres\n")

        #expect(word.original == "scope")
        #expect(word.translation == "zakres")
    }

    @Test func gradeRecomputesTheDueDateAndPersistsIt() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))
        let word = try manager.addWord(original: "scope", translation: "zakres", now: Self.now)

        #expect(try manager.grade(id: word.id, grade: .good, now: Self.now) == true)

        let fromDisk = try WordStore(fileURL: dir.jsonFile).word(id: word.id)
        #expect(fromDisk?.review.repetitions == 1)
    }

    @Test func gradeOfAnUnknownWordReturnsFalse() throws {
        let dir = try TempDirectory()
        let manager = WordManager(store: try WordStore(fileURL: dir.jsonFile))

        #expect(try manager.grade(id: UUID(), grade: .good) == false)
    }
}
