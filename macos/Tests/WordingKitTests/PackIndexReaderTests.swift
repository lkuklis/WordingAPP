import Foundation
import Testing

@testable import WordingKit

@Suite struct PackIndexReaderTests {
    static func data(_ json: String) -> Data { Data(json.utf8) }

    static let valid = """
        {
          "version": 1,
          "packs": [
            { "id": "spanish-travel", "name": "English → Spanish, travel",
              "description": "Airport words", "kind": "vocabulary", "wordCount": 25 },
            { "id": "it-interview-concepts", "name": "Backend interview concepts",
              "kind": "concepts", "wordCount": 44 }
          ]
        }
        """

    @Test func returnsEveryUsableEntry() throws {
        let entries = try PackIndexReader.read(Self.data(Self.valid))

        #expect(entries.count == 2)
        #expect(entries[0].id == "spanish-travel")
        #expect(entries[0].name == "English → Spanish, travel")
        #expect(entries[0].description == "Airport words")
        #expect(entries[0].wordCount == 25)
        #expect(entries[1].kind == PackKind.concepts)
    }

    @Test(arguments: ["../../words", "/etc/passwd", "https://elsewhere.example/evil", "con", ""])
    func dropsAnEntryWhoseIdentifierCouldChooseItsOwnAddress(id: String) throws {
        // The identifier is the whole of what decides the URL the app will fetch, so a
        // catalogue cannot smuggle one in.
        let json = """
            { "version": 1, "packs": [
              { "id": "\(id)", "name": "Bad", "wordCount": 1 },
              { "id": "spanish-travel", "name": "Good", "wordCount": 25 }] }
            """

        let entries = try PackIndexReader.read(Self.data(json))

        #expect(entries.count == 1)
        #expect(entries[0].id == "spanish-travel")
    }

    @Test func keepsTheGoodRowsWhenOneIsUnusable() throws {
        // A single bad entry must not hide the whole catalogue: it is the only way most
        // people will ever find a pack.
        let json = """
            { "version": 1, "packs": [
              { "id": "no-name", "name": "  ", "wordCount": 3 },
              { "id": "spanish-travel", "name": "Good", "wordCount": 25 }] }
            """

        let entries = try PackIndexReader.read(Self.data(json))

        #expect(entries.count == 1)
        #expect(entries[0].id == "spanish-travel")
    }

    @Test func keepsOnlyTheFirstOfADuplicatedIdentifier() throws {
        let json = """
            { "version": 1, "packs": [
              { "id": "spanish-travel", "name": "First", "wordCount": 1 },
              { "id": "Spanish-Travel", "name": "Second", "wordCount": 2 }] }
            """

        let entries = try PackIndexReader.read(Self.data(json))

        #expect(entries.count == 1)
        #expect(entries[0].name == "First")
    }

    @Test func tidiesTextThatWouldBreakTheList() throws {
        let json = """
            { "version": 1, "packs": [
              { "id": "x", "name": "two\\nlines ", "description": "\\ttabbed", "wordCount": -5 }] }
            """

        let entry = try #require(try PackIndexReader.read(Self.data(json)).first)

        #expect(entry.name == "two lines")
        #expect(entry.description == "tabbed")
        #expect(entry.wordCount == 0)
    }

    @Test func defaultsAMissingKindToVocabulary() throws {
        let json = #"{ "version": 1, "packs": [{ "id": "x", "name": "X", "wordCount": 1 }] }"#

        #expect(try PackIndexReader.read(Self.data(json)).first?.kind == PackKind.vocabulary)
    }

    @Test func stopsAtTheEntryLimit() throws {
        let rows = (0..<(PackLimits.maxIndexEntries + 50))
            .map { #"{ "id": "pack-\#($0)", "name": "P\#($0)", "wordCount": 1 }"# }
            .joined(separator: ",")

        let entries = try PackIndexReader.read(Self.data(#"{ "version": 1, "packs": [\#(rows)] }"#))

        #expect(entries.count == PackLimits.maxIndexEntries)
    }

    @Test func anEmptyCatalogueIsNotAnError() throws {
        // Nothing published yet is a state, not a failure.
        #expect(try PackIndexReader.read(Self.data(#"{ "version": 1, "packs": [] }"#)).isEmpty)
    }

    @Test(arguments: ["not json", "[]"])
    func refusesWhatIsNotACatalogue(json: String) {
        #expect(throws: WordPackError.self) { try PackIndexReader.read(Self.data(json)) }
    }

    @Test func refusesAPayloadOverTheLimit() {
        let padding = String(repeating: "x", count: PackLimits.maxPayloadBytes + 1)
        let json = #"{ "version": 1, "packs": [{ "id": "x", "name": "\#(padding)", "wordCount": 1 }] }"#

        #expect(throws: WordPackError.tooLarge) { try PackIndexReader.read(Self.data(json)) }
    }

    @Test func packUrlIsBuiltFromTheIndexAddressAndTheIdentifierAlone() throws {
        let index = URL(string: "https://example.com/data/index.json")!

        #expect(
            try PackSource.packURL(index: index, id: "spanish-travel").absoluteString
                == "https://example.com/data/spanish-travel.json")

        // A mirror serves the same catalogue without a single address being rewritten.
        #expect(
            try PackSource.packURL(
                index: URL(string: "https://mirror.example.org/wording/index.json")!,
                id: "spanish-travel"
            ).absoluteString == "https://mirror.example.org/wording/spanish-travel.json")
    }

    @Test func packUrlRefusesAnIdentifierThatIsNotASafeSlug() {
        let index = URL(string: "https://example.com/data/index.json")!

        #expect(throws: WordPackError.unsafeId) {
            try PackSource.packURL(index: index, id: "../../secrets")
        }
    }
}
