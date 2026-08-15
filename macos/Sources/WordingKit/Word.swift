import Foundation

public struct Word: Codable, Equatable, Identifiable, Sendable {
    /// GUID, zeby dwa urzadzenia dodajace slowko offline nie wygenerowaly tego samego identyfikatora.
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

    /// Slowko jeszcze nigdy nieocenione.
    public var isNew: Bool { review.lastReviewedUtc == nil }

    public func isDue(at now: Date) -> Bool { review.dueUtc <= now }

    enum CodingKeys: String, CodingKey {
        case id, original, translation, createdUtc, review
    }

    // Dekoder jest syntezowany - `UUID(uuidString:)` przyjmuje obie wielkosci liter.
    // Wlasny jest tylko koder, bo System.Text.Json zapisuje GUID-y malymi literami,
    // a Swift domyslnie wielkimi; bez tego pierwszy zapis z macOS przepisalby
    // wszystkie identyfikatory w pliku.
    public func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)

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
