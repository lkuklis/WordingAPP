import Foundation

/// A shareable set of words, as published at a URL.
///
/// Deliberately not the shape of words.json. That file is personal state - identifiers
/// and review progress - while a pack is only content. If they were the same type, a
/// published pack would carry its author's review history, and importing it would either
/// overwrite the reader's progress or invent one for them.
///
/// A port of `Wording.Core.Packs.WordPack`.
public struct WordPack: Codable, Equatable, Sendable {
    /// Becomes the file name of the imported set, so it is checked by `PackSlug`.
    public var id: String

    /// Shown before the import is confirmed, so the user knows what they are about to add.
    public var name: String

    public var description: String?

    /// "vocabulary" or "concepts" - see `PackKind`. Absent means vocabulary, so every
    /// pack written before this field existed still reads correctly.
    public var kind: String?

    public var words: [PackEntry]

    public init(
        id: String,
        name: String,
        description: String? = nil,
        kind: String? = nil,
        words: [PackEntry]
    ) {
        self.id = id
        self.name = name
        self.description = description
        self.kind = kind
        self.words = words
    }
}

/// One word in a pack: no identifier, no dates, no review state.
public struct PackEntry: Codable, Equatable, Sendable {
    public var original: String
    public var translation: String

    public init(original: String, translation: String) {
        self.original = original
        self.translation = translation
    }
}

/// Why a pack was rejected. The UI decides how to phrase each case.
public enum WordPackError: Error, Equatable, Sendable {
    /// The address was not https.
    case notHttps

    /// The address could not be reached, or the server answered with an error.
    case network(String)

    /// Larger than `PackLimits.maxPayloadBytes`, or too many words.
    case tooLarge

    /// Not JSON, or not the shape of a pack.
    case malformed(String)

    /// Parsed, but carried no usable word.
    case empty

    /// The identifier could not be turned into a safe file name.
    case unsafeId

    /// A set with this identifier is already on disk.
    case alreadyExists
}
