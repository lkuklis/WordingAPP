import Foundation
import Testing

@testable import WordingKit

/// Checks every pack published in learning_data/ with the parser the app uses.
///
/// These packs are meant to be contributed by anyone, so the only thing keeping the
/// directory honest is that a file which would be refused on someone's machine fails the
/// build here first. The .NET port runs the same check from its side - both have to
/// agree, since a pack is imported by whichever app the reader happens to run.
@Suite struct PublishedPackTests {
    /// Located from this source file rather than the working directory, which differs
    /// between `swift test`, Xcode and CI.
    static func directory(_ thisFile: String = #filePath) -> URL {
        URL(filePath: thisFile)
            .deletingLastPathComponent()  // .../macos/Tests/WordingKitTests
            .deletingLastPathComponent()  // .../macos/Tests
            .deletingLastPathComponent()  // .../macos
            .deletingLastPathComponent()  // repository root
            .appending(path: "learning_data", directoryHint: .isDirectory)
    }

    static let indexFileName = "index.json"

    /// Every pack in the directory. The catalogue itself is not one.
    static func published() throws -> [URL] {
        try FileManager.default
            .contentsOfDirectory(at: directory(), includingPropertiesForKeys: nil)
            .filter { $0.pathExtension == "json" && $0.lastPathComponent != indexFileName }
            .sorted { $0.lastPathComponent < $1.lastPathComponent }
    }

    @Test func theCatalogueMatchesThePacksOnDisk() throws {
        // The index is the one registry in this app, forced by the fact that a directory
        // cannot be listed over HTTP - so it is also the one thing that can silently stop
        // matching reality. Run learning_data/build-index.sh after changing a pack.
        let index = try PackIndexReader.read(
            try Data(contentsOf: Self.directory().appending(path: Self.indexFileName)))

        var onDisk: [String: WordPack] = [:]

        for file in try Self.published() {
            let pack = try WordPackReader.read(try Data(contentsOf: file))
            onDisk[pack.id] = pack
        }

        #expect(index.map(\.id).sorted() == onDisk.keys.sorted())

        for entry in index {
            let pack = try #require(onDisk[entry.id], "\(entry.id) is listed but not on disk")

            #expect(entry.name == pack.name)
            #expect(entry.kind == PackKind.normalize(pack.kind))
            #expect(entry.wordCount == pack.words.count)
            #expect(entry.description == pack.description)
        }
    }

    @Test func theOfficialCatalogueAddressPointsAtThisDirectory() throws {
        let index = try #require(URL(string: PackSource.officialIndexUrl))

        #expect(index.path().hasSuffix("/learning_data/\(Self.indexFileName)"))

        // And a pack address is derived from it, never taken from the file.
        #expect(
            try PackSource.packURL(index: index, id: "spanish-travel").absoluteString
                == "https://raw.githubusercontent.com/lkuklis/WordingAPP/master/learning_data/spanish-travel.json"
        )
    }

    @Test func theDirectoryIsWhereItIsExpected() throws {
        // Otherwise an empty loop below would pass while checking nothing at all.
        #expect(FileManager.default.fileExists(atPath: Self.directory().path(percentEncoded: false)))
        #expect(try !Self.published().isEmpty)
    }

    @Test func everyPublishedPackWouldImportCleanly() throws {
        for file in try Self.published() {
            let pack = try WordPackReader.read(try Data(contentsOf: file))

            // The file name has to match the id, or the set lands under a name nobody chose.
            #expect(pack.id == file.deletingPathExtension().lastPathComponent)
            #expect(!pack.words.isEmpty)
        }
    }

    @Test func theGeneratorPromptQuotesTheLimitsThatAreActuallyEnforced() throws {
        // learning_data/PROMPT.md tells contributors - and their AI - what a pack may
        // contain. A prompt that has drifted from PackLimits produces files that look
        // right and are refused on import, which is worse than having no prompt.
        let prompt = try String(contentsOf: Self.directory().appending(path: "PROMPT.md"), encoding: .utf8)

        for limit in [
            PackLimits.maxWords,
            PackLimits.maxFieldLength,
            PackLimits.maxNameLength,
            PackLimits.maxDescriptionLength,
            PackLimits.maxIdLength,
        ] {
            #expect(prompt.contains(String(limit)), "PROMPT.md never mentions \(limit)")
        }
    }

    @Test func theLimitsMatchTheDotNetPort() {
        // Spelled out rather than read from PackLimits, so changing one port fails here
        // and says plainly that Wording.Core/Packs/PackLimits.cs has to change with it.
        // The two apps import the same published packs; limits that disagree mean a pack
        // one of them accepts and the other refuses.
        #expect(PackLimits.maxPayloadBytes == 2 * 1024 * 1024)
        #expect(PackLimits.maxWords == 5_000)
        #expect(PackLimits.maxFieldLength == 200)
        #expect(PackLimits.maxNameLength == 80)
        #expect(PackLimits.maxDescriptionLength == 300)
        #expect(PackLimits.maxIdLength == 64)
        #expect(PackLimits.maxIndexEntries == 500)
    }

    @Test func noPublishedPackRepeatsAWord() throws {
        for file in try Self.published() {
            let pack = try WordPackReader.read(try Data(contentsOf: file))
            let originals = pack.words.map { $0.original.lowercased() }

            #expect(Set(originals).count == originals.count, "\(file.lastPathComponent) repeats a word")
        }
    }
}
