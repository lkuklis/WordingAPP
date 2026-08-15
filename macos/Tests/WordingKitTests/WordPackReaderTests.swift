import Foundation
import Testing

@testable import WordingKit

@Suite struct WordPackReaderTests {
    static let valid = """
        {
          "id": "travel-basics",
          "name": "Travel basics",
          "description": "Everyday phrases",
          "words": [
            { "original": "airport", "translation": "aeropuerto" },
            { "original": "ticket", "translation": "billete" }
          ]
        }
        """

    static func data(_ json: String) -> Data { Data(json.utf8) }

    @Test func acceptsAWellFormedPack() throws {
        let pack = try WordPackReader.read(Self.data(Self.valid))

        #expect(pack.id == "travel-basics")
        #expect(pack.name == "Travel basics")
        #expect(pack.description == "Everyday phrases")
        #expect(pack.words.count == 2)
        #expect(pack.words[0].translation == "aeropuerto")
    }

    @Test func keepsNonAsciiIntact() throws {
        let pack = try WordPackReader.read(
            Self.data("""
                { "id": "pl", "name": "Polski", "words": [
                  { "original": "default", "translation": "domyślnie" }] }
                """))

        #expect(pack.words[0].translation == "domyślnie")
    }

    @Test(arguments: ["not json at all", "{ \"id\": ", "[]"])
    func refusesWhatIsNotAPack(json: String) {
        #expect(throws: WordPackError.self) { try WordPackReader.read(Self.data(json)) }
    }

    @Test func refusesAPackThatChoosesItsOwnFileName() {
        let json = Self.valid.replacingOccurrences(of: "travel-basics", with: "../../words")

        #expect(throws: WordPackError.unsafeId) { try WordPackReader.read(Self.data(json)) }
    }

    @Test func refusesAPackWithNoName() {
        let json = Self.valid.replacingOccurrences(of: "Travel basics", with: "   ")

        #expect(throws: WordPackError.malformed("the pack has no name")) {
            try WordPackReader.read(Self.data(json))
        }
    }

    @Test func refusesAPayloadOverTheLimit() {
        // Padding inside the description, so the file stays valid JSON.
        let padding = String(repeating: "x", count: PackLimits.maxPayloadBytes + 1)
        let json = Self.valid.replacingOccurrences(of: "Everyday phrases", with: padding)

        #expect(throws: WordPackError.tooLarge) { try WordPackReader.read(Self.data(json)) }
    }

    @Test func refusesTooManyWords() {
        let entries = (0...PackLimits.maxWords)
            .map { #"{ "original": "w\#($0)", "translation": "t\#($0)" }"# }
            .joined(separator: ",")

        #expect(throws: WordPackError.tooLarge) {
            try WordPackReader.read(Self.data(#"{ "id": "big", "name": "Big", "words": [\#(entries)] }"#))
        }
    }

    @Test func refusesAFieldTooLongToBeAWord() {
        let essay = String(repeating: "a", count: PackLimits.maxFieldLength + 1)
        let json = #"{ "id": "x", "name": "X", "words": [{ "original": "\#(essay)", "translation": "t" }] }"#

        #expect(throws: WordPackError.self) { try WordPackReader.read(Self.data(json)) }
    }

    @Test func skipsBlankEntriesButKeepsTheRest() throws {
        let json = """
            { "id": "x", "name": "X", "words": [
              { "original": "  ", "translation": "empty" },
              { "original": "keep", "translation": "" },
              { "original": "airport", "translation": "aeropuerto" }] }
            """

        let pack = try WordPackReader.read(Self.data(json))

        #expect(pack.words.count == 1)
        #expect(pack.words[0].original == "airport")
    }

    @Test func refusesAPackWhereNothingUsableIsLeft() {
        let json = #"{ "id": "x", "name": "X", "words": [{ "original": " ", "translation": " " }] }"#

        #expect(throws: WordPackError.empty) { try WordPackReader.read(Self.data(json)) }
    }

    @Test func refusesAPackWithNoWordsAtAll() {
        #expect(throws: WordPackError.empty) {
            try WordPackReader.read(Self.data(#"{ "id": "x", "name": "X", "words": [] }"#))
        }
    }

    @Test func foldsControlCharactersThatWouldReachANotification() throws {
        let json = """
            { "id": "x", "name": "X", "words": [
              { "original": "two\\nlines", "translation": "\\ttabbed " }] }
            """

        let word = try WordPackReader.read(Self.data(json)).words[0]

        #expect(word.original == "two lines")
        #expect(word.translation == "tabbed")
    }

    @Test func truncatesTheNameRatherThanRefusingThePack() throws {
        // Display-only, and the reader cannot expect the user to fix someone else's file.
        let json = Self.valid.replacingOccurrences(
            of: "Travel basics",
            with: String(repeating: "n", count: 200))

        #expect(try WordPackReader.read(Self.data(json)).name.count == PackLimits.maxNameLength)
    }
}
