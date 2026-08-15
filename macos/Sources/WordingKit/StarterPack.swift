import Foundation

/// Startowy pakiet slowek EN->PL dolaczony do aplikacji.
///
/// Odpowiednik importu z WordsData.xml po stronie .NET, tyle ze w JSON -
/// port na macOS celowo nie ma parsera starego formatu XML.
public enum StarterPack {
    struct Entry: Decodable {
        let original: String
        let translation: String
    }

    struct File: Decodable {
        let words: [Entry]
    }

    public static let resourceName = "starter-pack"

    /// Wczytuje pakiet startowy z zasobow pakietu.
    public static func load() throws -> [(original: String, translation: String)] {
        guard let url = Bundle.module.url(forResource: resourceName, withExtension: "json") else {
            return []
        }

        let file = try JSONDecoder().decode(File.self, from: Data(contentsOf: url))

        return file.words.map { ($0.original, $0.translation) }
    }
}

extension WordStore {
    /// Zasiewa magazyn pakietem startowym, ale wylacznie gdy jest pusty.
    /// Nigdy nie nadpisuje danych, ktore juz sa - takze tych zapisanych
    /// przez aplikacje .NET, bo obie pracuja na jednym pliku.
    /// - Returns: liczba dodanych slowek.
    @discardableResult
    public func seedIfEmpty(now: Date = Date()) throws -> Int {
        guard words.isEmpty else { return 0 }

        let pack = try StarterPack.load()

        guard !pack.isEmpty else { return 0 }

        // Jeden zapis zamiast jednego na slowko.
        try append(pack.map {
            Word(original: $0.original, translation: $0.translation, createdUtc: now, review: .new(now: now))
        })

        return pack.count
    }
}
