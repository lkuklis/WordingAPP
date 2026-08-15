import Foundation
import Testing

@testable import WordingKit

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
