import Foundation

/// Konfiguracja JSON zgodna z tym, co zapisuje System.Text.Json po stronie .NET.
///
/// Daty wymagaja uwagi: .NET zapisuje `DateTimeOffset` jako
/// `2026-08-14T22:18:18.405614+00:00`, czyli z szescioma cyframi ulamka sekundy.
/// Standardowa strategia `.iso8601` w Swift w ogole tego nie przyjmuje.
///
/// Kuszacy `Date.ISO8601FormatStyle` (typ wartosciowy, `Sendable`) tu NIE dziala:
/// przy zapisie ucina ulamek do milisekund zamiast zaokraglac, przez co runda
/// odczyt-zapis-odczyt nie jest stabilna (.405614 -> .405 -> .404) i kazdy zapis
/// przesuwalby znaczniki czasu w tyl. `ISO8601DateFormatter` zaokragla poprawnie.
///
/// Formater powstaje wewnatrz domkniecia, a nie jest do niego przechwytywany:
/// strategie kodowania sa `@Sendable`, a `ISO8601DateFormatter` nie jest `Sendable`.
/// Kosztuje to ok. 14 ms na zapis calego pliku - przy kilkudziesieciu slowkach
/// niezauwazalne, a unika dzielenia niebezpiecznego stanu miedzy watkami.
public enum WordingJSON {
    static func makeFormatter(fractionalSeconds: Bool) -> ISO8601DateFormatter {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions =
            fractionalSeconds
            ? [.withInternetDateTime, .withFractionalSeconds]
            : [.withInternetDateTime]
        return formatter
    }

    public static func parseDate(_ text: String) -> Date? {
        makeFormatter(fractionalSeconds: true).date(from: text)
            ?? makeFormatter(fractionalSeconds: false).date(from: text)
    }

    public static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
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
    }()

    public static let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes]
        encoder.dateEncodingStrategy = .custom { date, encoder in
            var container = encoder.singleValueContainer()
            // .NET przyjmuje zarowno "Z", jak i "+00:00".
            try container.encode(makeFormatter(fractionalSeconds: true).string(from: date))
        }
        return encoder
    }()
}
