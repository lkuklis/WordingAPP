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

    static func published() throws -> [URL] {
        try FileManager.default
            .contentsOfDirectory(at: directory(), includingPropertiesForKeys: nil)
            .filter { $0.pathExtension == "json" }
            .sorted { $0.lastPathComponent < $1.lastPathComponent }
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

    @Test func noPublishedPackRepeatsAWord() throws {
        for file in try Self.published() {
            let pack = try WordPackReader.read(try Data(contentsOf: file))
            let originals = pack.words.map { $0.original.lowercased() }

            #expect(Set(originals).count == originals.count, "\(file.lastPathComponent) repeats a word")
        }
    }
}
