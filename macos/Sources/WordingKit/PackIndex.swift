import Foundation

/// The catalogue of packs published alongside the app, so the user can pick one from a
/// list instead of pasting an address.
///
/// It exists because a directory cannot be listed over plain HTTP. Everywhere else in this
/// app a listing beats a registry - the set catalogue reads the directory precisely so it
/// cannot disagree with the disk - but there is no remote equivalent, so the index is
/// written down and a test keeps it in step with the files.
///
/// A port of `Wording.Core.Packs.PackIndex`.
struct PackIndex: Codable {
    var version: Int
    var packs: [PackIndexEntry]
}

/// One row in the catalogue: enough to show the user what a pack is before downloading it.
///
/// Deliberately carries no address and no file name. The app builds the URL itself from
/// `id` and the address the index was fetched from, so a file downloaded from the internet
/// cannot point the app at somewhere else. It also makes the catalogue work unchanged in a
/// fork or a mirror, because every address is relative to the index.
public struct PackIndexEntry: Codable, Equatable, Identifiable, Sendable {
    public var id: String
    public var name: String
    public var description: String?

    /// See `PackKind`. Absent means vocabulary.
    public var kind: String?

    /// Shown in the list. The pack itself decides what actually gets imported.
    public var wordCount: Int

    public init(
        id: String,
        name: String,
        description: String? = nil,
        kind: String? = nil,
        wordCount: Int = 0
    ) {
        self.id = id
        self.name = name
        self.description = description
        self.kind = kind
        self.wordCount = wordCount
    }
}

/// Where the published catalogue lives, and how a pack's address is derived from it.
///
/// A port of `Wording.Core.Packs.PackSource`.
public enum PackSource {
    /// The catalogue in this repository, on the default branch. Pointing at a branch
    /// rather than a tag is deliberate: a pack added to the repository then shows up in
    /// versions of the app that are already installed, so the catalogue grows without a
    /// release. The other half of that bargain is that a broken index breaks the browse
    /// window for everyone at once, which is why CI validates it.
    public static let officialIndexUrl =
        "https://raw.githubusercontent.com/lkuklis/WordingAPP/master/learning_data/index.json"

    /// The address of a pack listed in an index.
    ///
    /// Built from the identifier and the index's own address, never from anything the
    /// index says. That is what stops a downloaded catalogue from sending the app
    /// somewhere else, and it is why the same file works in a fork without editing.
    public static func packURL(index: URL, id: String) throws -> URL {
        let slug = try PackSlug.require(id)

        guard let url = URL(string: "\(slug).json", relativeTo: index)?.absoluteURL else {
            throw WordPackError.unsafeId
        }

        return url
    }
}
