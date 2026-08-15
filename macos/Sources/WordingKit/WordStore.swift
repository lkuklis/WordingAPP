import Foundation

/// Stores words in a JSON file.
///
/// A port of `Wording.Core.Storage.JsonWordStore`. Saving is atomic (temporary file,
/// then replace), so interrupting the process cannot leave the user with a truncated
/// data file.
public final class WordStore {
    public let fileURL: URL
    public private(set) var words: [Word] = []

    public init(fileURL: URL = WordingPaths.dataFile()) throws {
        self.fileURL = fileURL
        try reload()
    }

    /// Re-reads from disk, discarding the in-memory state.
    public func reload() throws {
        guard FileManager.default.fileExists(atPath: fileURL.path(percentEncoded: false)) else {
            words = []
            return
        }

        let data = try Data(contentsOf: fileURL)

        guard !data.isEmpty else {
            words = []
            return
        }

        words = try WordingJSON.decoder.decode(WordFile.self, from: data).words
    }

    public func word(id: UUID) -> Word? {
        words.first { $0.id == id }
    }

    @discardableResult
    public func add(original: String, translation: String, now: Date) throws -> Word {
        let word = Word(
            original: original,
            translation: translation,
            createdUtc: now,
            review: .new(now: now)
        )

        words.append(word)
        try save()

        return word
    }

    @discardableResult
    public func remove(id: UUID) throws -> Bool {
        let before = words.count
        words.removeAll { $0.id == id }

        guard words.count != before else { return false }

        try save()
        return true
    }

    /// Persists changes to a word already in the store (for example after grading).
    @discardableResult
    public func update(_ word: Word) throws -> Bool {
        guard let index = words.firstIndex(where: { $0.id == word.id }) else {
            return false
        }

        words[index] = word
        try save()

        return true
    }

    private func save() throws {
        let directory = fileURL.deletingLastPathComponent()
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)

        let payload = WordFile(version: WordFile.currentVersion, words: words)
        let data = try WordingJSON.encoder.encode(payload)

        // Write alongside, then swap - a crash mid-write leaves the original intact.
        let temporary = fileURL.appendingPathExtension("tmp")
        try data.write(to: temporary, options: .atomic)
        _ = try FileManager.default.replaceItemAt(fileURL, withItemAt: temporary)
    }
}
