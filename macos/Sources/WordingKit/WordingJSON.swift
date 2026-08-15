import Foundation

/// JSON settings that match what System.Text.Json writes on the .NET side.
///
/// Dates need care: .NET writes `DateTimeOffset` as `2026-08-14T22:18:18.405614+00:00`,
/// with six fractional-second digits. Swift's stock `.iso8601` strategy rejects
/// fractional seconds outright.
///
/// The tempting `Date.ISO8601FormatStyle` (a value type, and `Sendable`) does NOT work
/// here: it truncates the fraction to milliseconds instead of rounding, so the
/// read-write-read round trip does not converge (.405614 -> .405 -> .404) and every save
/// would walk timestamps backwards. `ISO8601DateFormatter` rounds correctly.
///
/// The formatter is built inside each coding closure rather than captured by it: the
/// strategies are `@Sendable` and `ISO8601DateFormatter` is not. That costs about 14 ms
/// per whole-file save - imperceptible at this size, and it avoids sharing unsafe state
/// across threads.
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
                    debugDescription: "Unrecognised date format: \(text)"
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
            // .NET accepts both "Z" and "+00:00".
            try container.encode(makeFormatter(fractionalSeconds: true).string(from: date))
        }
        return encoder
    }()
}
