import Foundation

/// Startowy pakiet slowek EN->PL dolaczony do aplikacji.
///
/// Odpowiednik tego, co po stronie .NET robi import z WordsData.xml, tyle ze
/// w JSON - port na macOS celowo nie ma parsera starego formatu XML.
/// Sluzy wylacznie do zasiania pustego magazynu przy pierwszym uruchomieniu.
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
    /// przez powloke .NET, bo obie aplikacje pracuja na jednym pliku.
    /// - Returns: liczba dodanych slowek.
    @discardableResult
    public func seedIfEmpty(now: Date = Date()) throws -> Int {
        guard words.isEmpty else { return 0 }

        let pakiet = try StarterPack.load()

        guard !pakiet.isEmpty else { return 0 }

        for wpis in pakiet {
            _ = try add(original: wpis.original, translation: wpis.translation, now: now)
        }

        return pakiet.count
    }
}
