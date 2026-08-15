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

    /// A set file as System.Text.Json writes one, header included.
    static let dotNetSetFile = """
        {
          "version": 1,
          "set": {
            "id": "travel-basics",
            "name": "Travel basics",
            "sourceUrl": "https://example.com/travel-basics.json",
            "importedUtc": "2026-08-15T09:31:02.117433+00:00"
          },
          "words": [
            {
              "id": "3b2f22dc-8173-44cd-c79f-976fd4fffb1f",
              "original": "airport",
              "translation": "aeropuerto",
              "createdUtc": "2026-08-15T09:31:02.117433+00:00",
              "review": {
                "repetitions": 0,
                "intervalDays": 0,
                "easeFactor": 2.5,
                "dueUtc": "2026-08-15T09:31:02.117433+00:00",
                "lapses": 0
              }
            }
          ]
        }
        """

    @Test func readsASetFileWrittenByDotNet() throws {
        let file = try WordingJSON.decoder.decode(WordFile.self, from: Data(Self.dotNetSetFile.utf8))
        let set = try #require(file.set)

        #expect(set.id == "travel-basics")
        #expect(set.name == "Travel basics")
        #expect(set.sourceUrl == "https://example.com/travel-basics.json")
        #expect(file.words.count == 1)
    }

    @Test func theSetHeaderSurvivesARoundTrip() throws {
        // Grading rewrites the whole file, so a header lost here is a set that can no
        // longer be named or refreshed.
        let original = try WordingJSON.decoder.decode(WordFile.self, from: Data(Self.dotNetSetFile.utf8))

        let encoded = try WordingJSON.encoder.encode(original)
        let decoded = try WordingJSON.decoder.decode(WordFile.self, from: encoded)

        #expect(decoded.set == original.set)
    }

    @Test func aFileWithNoSetHeaderStaysWithoutOne() throws {
        // words.json must not grow a "set" key just by being saved.
        let file = try WordingJSON.decoder.decode(WordFile.self, from: Data(Self.dotNetFile.utf8))
        let json = String(data: try WordingJSON.encoder.encode(file), encoding: .utf8)!

        #expect(file.set == nil)
        #expect(!json.contains("\"set\""))
    }

    @Test func readsAPackInTheSharedFormat() throws {
        // The pack format is the other half of the contract: both apps read the same
        // published file, so the key names have to match exactly.
        let published = """
            {
              "id": "travel-basics",
              "name": "Travel basics",
              "description": "Everyday phrases",
              "words": [{ "original": "airport", "translation": "aeropuerto" }]
            }
            """

        let pack = try WordPackReader.read(Data(published.utf8))

        #expect(pack.id == "travel-basics")
        #expect(pack.description == "Everyday phrases")
        #expect(pack.words == [PackEntry(original: "airport", translation: "aeropuerto")])
    }

    @Test func theSetsDirectoryMatchesTheDotNetOne() {
        let path = WordingPaths.setFile("travel-basics").path(percentEncoded: false)

        #expect(path.hasSuffix("Library/Application Support/Wording/sets/travel-basics.json"))
    }

    @Test func theDataDirectoryMatchesTheDotNetOne() {
        let path = WordingPaths.dataFile().path(percentEncoded: false)

        #expect(path.hasSuffix("Library/Application Support/Wording/words.json"))
    }
}
