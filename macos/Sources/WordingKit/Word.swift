import Foundation

public struct Word: Codable, Equatable, Identifiable, Sendable {
    /// A GUID, so two devices adding a word offline cannot produce the same identifier.
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

    /// A word that has never been graded.
    public var isNew: Bool { review.lastReviewedUtc == nil }

    public func isDue(at now: Date) -> Bool { review.dueUtc <= now }

    enum CodingKeys: String, CodingKey {
        case id, original, translation, createdUtc, review
    }

    // The decoder is synthesized - `UUID(uuidString:)` accepts either case. Only the
    // encoder is hand-written, because System.Text.Json writes GUIDs in lower case
    // while Swift defaults to upper case; without this the first save from macOS
    // would rewrite every identifier in the file.
    public func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)

        try container.encode(id.uuidString.lowercased(), forKey: .id)
        try container.encode(original, forKey: .original)
        try container.encode(translation, forKey: .translation)
        try container.encode(createdUtc, forKey: .createdUtc)
        try container.encode(review, forKey: .review)
    }
}

/// Shape of words.json - must match `Wording.Core.Storage.WordFile`.
struct WordFile: Codable {
    var version: Int
    var words: [Word]

    static let currentVersion = 1
}
