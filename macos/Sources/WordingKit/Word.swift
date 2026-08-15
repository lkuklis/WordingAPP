import Foundation

public struct Word: Codable, Equatable, Identifiable, Sendable {
    /// GUID, tak jak w wersji .NET - dwa urzadzenia dodajace slowko offline
    /// nie moga wygenerowac tego samego identyfikatora.
    public let id: UUID
    public var original: String
    public var translation: String
    public let createdUtc: Date
    public var review: ReviewState

    public init(
        id: UUID = UUID(),
        original: String,
        translation: String,
        createdUtc: Date,
        review: ReviewState
    ) {
        self.id = id
        self.original = original
        self.translation = translation
        self.createdUtc = createdUtc
        self.review = review
    }

    enum CodingKeys: String, CodingKey {
        case id, original, translation, createdUtc, review
    }

    public init(from decoder: any Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        let rawId = try container.decode(String.self, forKey: .id)

        guard let parsed = UUID(uuidString: rawId) else {
            throw DecodingError.dataCorruptedError(
                forKey: .id,
                in: container,
                debugDescription: "Niepoprawny GUID: \(rawId)"
            )
        }

        id = parsed
        original = try container.decode(String.self, forKey: .original)
        translation = try container.decode(String.self, forKey: .translation)
        createdUtc = try container.decode(Date.self, forKey: .createdUtc)
        review = try container.decode(ReviewState.self, forKey: .review)
    }

    public func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)

        // System.Text.Json zapisuje GUID-y malymi literami, a Swift domyslnie
        // wielkimi. Bez tego kazdy zapis z macOS przepisywalby caly plik.
        try container.encode(id.uuidString.lowercased(), forKey: .id)
        try container.encode(original, forKey: .original)
        try container.encode(translation, forKey: .translation)
        try container.encode(createdUtc, forKey: .createdUtc)
        try container.encode(review, forKey: .review)
    }
}

/// Ksztalt pliku words.json - musi odpowiadac `Wording.Core.Storage.WordFile`.
struct WordFile: Codable {
    var version: Int
    var words: [Word]

    static let currentVersion = 1
}
