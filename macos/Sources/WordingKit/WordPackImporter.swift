import Foundation

/// What an import did, so the UI can report it without recounting.
public struct PackImportResult: Equatable, Sendable {
    public let set: WordSetInfo
    public let added: Int
    public let skipped: Int
}

/// Writes a validated pack into its own set file.
///
/// An import never touches words.json and never touches another set: each pack gets a
/// file of its own, so importing cannot disturb whatever the user is learning from at
/// the time.
///
/// A port of `Wording.Core.Packs.WordPackImporter`.
public struct WordPackImporter {
    private let setsDirectory: URL

    public init(setsDirectory: URL? = nil) {
        self.setsDirectory = setsDirectory ?? WordingPaths.setsDirectory()
    }

    /// Where the pack would be written, without writing it.
    public func path(for pack: WordPack) throws -> URL {
        WordingPaths.setFile(try PackSlug.require(pack.id), in: setsDirectory)
    }

    public func exists(_ pack: WordPack) -> Bool {
        guard let url = try? path(for: pack) else { return false }

        return FileManager.default.fileExists(atPath: url.path(percentEncoded: false))
    }

    /// Imports the pack. With `replaceExisting` false an existing set is refused rather
    /// than overwritten; with it true the pack is merged into that set, which adds the
    /// words it does not have yet and leaves the review progress of the ones it does
    /// completely alone.
    @discardableResult
    public func `import`(
        _ pack: WordPack,
        from source: URL?,
        replaceExisting: Bool = false,
        now: Date = Date()
    ) throws -> PackImportResult {
        let fileURL = try path(for: pack)

        if FileManager.default.fileExists(atPath: fileURL.path(percentEncoded: false)), !replaceExisting {
            throw WordPackError.alreadyExists
        }

        try FileManager.default.createDirectory(at: setsDirectory, withIntermediateDirectories: true)

        let store = try WordStore(fileURL: fileURL)
        let added = try merge(pack, into: store, now: now)

        try store.describe(
            WordSet(
                id: pack.id,
                name: pack.name,
                sourceUrl: source?.absoluteString,
                importedUtc: now
            )
        )

        guard let info = WordSetCatalog.read(fileURL) else {
            throw WordPackError.malformed("the set could not be read back")
        }

        return PackImportResult(set: info, added: added, skipped: pack.words.count - added)
    }

    /// Adds the words the set does not already hold. Matching is on the pair of word and
    /// translation, trimmed and ignoring case, which makes re-importing the same pack
    /// harmless - the point being that an existing word keeps its review state, since
    /// resetting someone's progress is the one thing an import must never do.
    private func merge(_ pack: WordPack, into store: WordStore, now: Date) throws -> Int {
        var seen = Set(store.words.map { key($0.original, $0.translation) })

        // insert() doubles as the duplicate test, so a pack that repeats a word
        // internally only contributes it once.
        let fresh = pack.words
            .filter { seen.insert(key($0.original, $0.translation)).inserted }
            .map {
                Word(
                    original: $0.original,
                    translation: $0.translation,
                    createdUtc: now,
                    review: .new(now: now)
                )
            }

        if !fresh.isEmpty {
            try store.addMany(fresh)
        }

        return fresh.count
    }

    private func key(_ original: String, _ translation: String) -> String {
        original.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
            + " "
            + translation.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
    }
}
