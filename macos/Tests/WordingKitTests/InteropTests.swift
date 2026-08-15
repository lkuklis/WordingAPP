import Foundation
import Testing

@testable import WordingKit

/// Format compatibility with the .NET app. This is the most important suite in the
/// package: words.json is the only contract between the macOS app and the .NET one,
/// so any serialization mismatch silently corrupts the user's data.
@Suite struct InteropTests {

    /// A verbatim fragment of a file written by System.Text.Json, including review
    /// state produced by real grading.
    static let dotNetFile = """
        {
          "version": 1,
          "words": [
            {
              "id": "2a1e11cb-7062-43bc-b68e-865fc3efea0e",
              "original": "scope",
              "translation": "zakres",
              "createdUtc": "2026-08-14T22:18:18.405614+00:00",
              "review": {
                "repetitions": 1,
                "intervalDays": 1,
                "easeFactor": 2.36,
                "dueUtc": "2026-08-15T22:18:43.812289+00:00",
                "lastReviewedUtc": "2026-08-14T22:18:43.812289+00:00",
                "lapses": 0
              }
            },
            {
              "id": "7fc14c29-429f-4217-86a0-0e9bc4f0d511",
              "original": "implicitly",
              "translation": "domyślnie, bezwzględnie, bez zastrzeżeń",
              "createdUtc": "2026-08-14T22:18:18.405614+00:00",
              "review": {
                "repetitions": 0,
                "intervalDays": 0,
                "easeFactor": 2.5,
                "dueUtc": "2026-08-14T22:18:18.405614+00:00",
                "lapses": 0
              }
            }
          ]
        }
        """

    @Test func readsAFileWrittenByDotNet() throws {
        let file = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.dotNetFile.utf8)
        )

        #expect(file.version == 1)
        #expect(file.words.count == 2)

        let scope = file.words[0]
        #expect(scope.original == "scope")
        #expect(scope.translation == "zakres")
        #expect(scope.id == UUID(uuidString: "2a1e11cb-7062-43bc-b68e-865fc3efea0e"))
        #expect(scope.review.repetitions == 1)
        #expect(scope.review.easeFactor == 2.36)
        #expect(scope.review.lastReviewedUtc != nil)
    }

    @Test func handlesDatesWithSixFractionalDigits() throws {
        // .NET writes microseconds; Swift's stock .iso8601 strategy rejects them.
        let parsed = WordingJSON.parseDate("2026-08-14T22:18:18.405614+00:00")

        #expect(parsed != nil)
    }

    @Test func missingLastReviewedUtcMeansNeverReviewed() throws {
        let file = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.dotNetFile.utf8)
        )

        // .NET omits the key instead of writing null (DefaultIgnoreCondition).
        #expect(file.words[1].review.lastReviewedUtc == nil)
    }

    @Test func encodingOmitsLastReviewedUtcJustLikeDotNet() throws {
        let word = Word(
            original: "scope",
            translation: "zakres",
            createdUtc: Date(),
            review: .new(now: Date())
        )

        let json = String(
            data: try WordingJSON.encoder.encode(WordFile(version: 1, words: [word])),
            encoding: .utf8
        )!

        #expect(!json.contains("lastReviewedUtc"))
    }

    @Test func encodesGuidsInLowerCase() throws {
        // Swift encodes UUIDs in upper case by default; without the fix, every save
        // from macOS would rewrite all identifiers in the file.
        let word = Word(
            id: UUID(uuidString: "2A1E11CB-7062-43BC-B68E-865FC3EFEA0E")!,
            original: "scope",
            translation: "zakres",
            createdUtc: Date(),
            review: .new(now: Date())
        )

        let json = String(
            data: try WordingJSON.encoder.encode(WordFile(version: 1, words: [word])),
            encoding: .utf8
        )!

        #expect(json.contains("2a1e11cb-7062-43bc-b68e-865fc3efea0e"))
        #expect(!json.contains("2A1E11CB"))
    }

    @Test func aFullRoundTripLosesNothing() throws {
        let original = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.dotNetFile.utf8)
        )

        let encoded = try WordingJSON.encoder.encode(original)
        let decoded = try WordingJSON.decoder.decode(WordFile.self, from: encoded)

        #expect(decoded.words == original.words)
        #expect(decoded.words[1].translation == "domyślnie, bezwzględnie, bez zastrzeżeń")
    }

    @Test func theDataDirectoryMatchesTheDotNetOne() {
        let path = WordingPaths.dataFile().path(percentEncoded: false)

        #expect(path.hasSuffix("Library/Application Support/Wording/words.json"))
    }
}
