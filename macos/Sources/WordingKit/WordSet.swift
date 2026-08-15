import Foundation

/// Header of an imported set, stored inside its own file.
///
/// Absent from words.json: the user's own words are not an import and have no source.
/// Must match `Wording.Core.Storage.WordSet`.
public struct WordSet: Codable, Equatable, Sendable {
    public var id: String
    public var name: String

    /// Where it came from, so the set can be refreshed later.
    public var sourceUrl: String?

    /// Carried over from the pack - see `PackKind`.
    public var kind: String?

    public var importedUtc: Date

    public init(
        id: String,
        name: String,
        sourceUrl: String? = nil,
        kind: String? = nil,
        importedUtc: Date
    ) {
        self.id = id
        self.name = name
        self.sourceUrl = sourceUrl
        self.kind = kind
        self.importedUtc = importedUtc
    }
}

/// One entry in the list of installed sets. The word count is read from the file rather
/// than stored in it - a stored count starts lying the moment a word is deleted.
public struct WordSetInfo: Equatable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let sourceUrl: String?
    public let kind: String
    public let wordCount: Int
    public let fileURL: URL

    public init(
        id: String,
        name: String,
        sourceUrl: String?,
        kind: String,
        wordCount: Int,
        fileURL: URL
    ) {
        self.id = id
        self.name = name
        self.sourceUrl = sourceUrl
        self.kind = kind
        self.wordCount = wordCount
        self.fileURL = fileURL
    }
}
