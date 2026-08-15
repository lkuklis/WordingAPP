import Foundation
import Testing

@testable import WordingKit

@Suite struct StarterPackTests {
    static let now = Fixtures.now

    @Test func starterPackShipsWithTheBundle() throws {
        let pack = try StarterPack.load()

        #expect(pack.count == 38)
        #expect(pack.contains { $0.original == "scope" && $0.translation == "zakres" })
    }

    @Test func starterPackHasNoBlankEntries() throws {
        for entry in try StarterPack.load() {
            #expect(!entry.original.isEmpty)
            #expect(!entry.translation.isEmpty)
        }
    }

    @Test func starterPackKeepsNonAsciiCharacters() throws {
        #expect(try StarterPack.load().contains { $0.translation.contains("domyślnie") })
    }

    @Test func seedsAnEmptyStore() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        #expect(try store.seedIfEmpty(now: Self.now) == 38)
        #expect(store.words.count == 38)

        // All due immediately and never graded.
        for word in store.words {
            #expect(word.isNew)
            #expect(word.isDue(at: Self.now))
        }
    }

    @Test func seededWordsGetUniqueIdentifiers() throws {
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        try store.seedIfEmpty(now: Self.now)

        #expect(Set(store.words.map(\.id)).count == 38)
    }

    @Test func leavesANonEmptyStoreAlone() throws {
        // Critical: the file may come from the .NET app and carry review state.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)
        try store.add(original: "already-here", translation: "existing", now: Self.now)

        #expect(try store.seedIfEmpty(now: Self.now) == 0)
        #expect(store.words.count == 1)
        #expect(store.words[0].original == "already-here")
    }

    @Test func aSeededStoreCanBeReadBackFromDisk() throws {
        let dir = try TempDirectory()
        try WordStore(fileURL: dir.jsonFile).seedIfEmpty(now: Self.now)

        #expect(try WordStore(fileURL: dir.jsonFile).words.count == 38)
    }

    @Test func seedingWritesOnceNotOncePerWord() throws {
        // Every word used to go through its own add(), meaning 38 full file writes.
        let dir = try TempDirectory()
        let store = try WordStore(fileURL: dir.jsonFile)

        try store.seedIfEmpty(now: Self.now)

        // All words share one timestamp - the trace of a single pass.
        #expect(Set(store.words.map(\.createdUtc)).count == 1)
    }
}
