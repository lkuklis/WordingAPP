import Foundation

/// Konfiguracja JSON zgodna z tym, co zapisuje System.Text.Json po stronie .NET.
///
/// Newralgiczne sa daty: .NET zapisuje `DateTimeOffset` jako
/// `2026-08-14T22:18:18.405614+00:00`, czyli z szescioma cyframi ulamka sekundy
/// i jawnym przesunieciem. Standardowe `.iso8601` w Swift tego nie przyjmuje,
/// bo nie obsluguje czesci ulamkowej - stad wlasna strategia z fallbackiem.
///
/// Kodery sa wlasciwosciami obliczanymi, a nie statycznymi stalymi: ani
/// `JSONEncoder`, ani `ISO8601DateFormatter` nie sa `Sendable`, wiec wspoldzielona
/// instancja nie przechodzi kontroli wspolbieznosci Swift 6. Plik ma kilkadziesiat
/// pozycji, wiec koszt tworzenia formatera jest bez znaczenia.
public enum WordingJSON {
    static func fractionalFormatter() -> ISO8601DateFormatter {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter
    }

    static func plainFormatter() -> ISO8601DateFormatter {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime]
        return formatter
    }

    public static func parseDate(_ text: String) -> Date? {
        fractionalFormatter().date(from: text) ?? plainFormatter().date(from: text)
    }

    public static var decoder: JSONDecoder {
        let decoder = JSONDecoder()
        // Formater powstaje wewnatrz domkniecia, a nie jest do niego przechwytywany:
        // strategia jest @Sendable, a ISO8601DateFormatter nie jest Sendable.
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let text = try container.decode(String.self)

            guard let date = parseDate(text) else {
                throw DecodingError.dataCorruptedError(
                    in: container,
                    debugDescription: "Nierozpoznany format daty: \(text)"
                )
            }

            return date
        }
        return decoder
    }

    public static var encoder: JSONEncoder {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes]
        encoder.dateEncodingStrategy = .custom { date, encoder in
            var container = encoder.singleValueContainer()
            // .NET przyjmuje zarowno "Z", jak i "+00:00", wiec zapis w tej
            // postaci jest bezpieczny w obie strony.
            try container.encode(fractionalFormatter().string(from: date))
        }
        return encoder
    }
}
