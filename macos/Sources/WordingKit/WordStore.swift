import Foundation

/// Stores words in a JSON file.
///
/// A port of `Wording.Core.Storage.JsonWordStore`. Saving is atomic (temporary file,
/// then replace), so interrupting the process cannot leave the user with a truncated
/// data file.
public final class WordStore {
    public let fileURL: URL
    public private(set) var words: [Word] = []

    /// The set header when this file is an imported set, nil for the user's own words.
    /// Held so that saving a grade cannot quietly drop it.
    public private(set) var set: WordSet?

    public init(fileURL: URL = WordingPaths.dataFile()) throws {
        self.fileURL = fileURL
        try reload()
    }

    /// Re-reads from disk, discarding the in-memory state.
    public func reload() throws {
        guard FileManager.default.fileExists(atPath: fileURL.path(percentEncoded: false)) else {
            words = []
            set = nil
            return
        }

        let data = try Data(contentsOf: fileURL)

        guard !data.isEmpty else {
            words = []
            set = nil
            return
        }

        let file = try WordingJSON.decoder.decode(WordFile.self, from: data)

        words = file.words
        set = file.set
    }

    /// Marks this file as an imported set, or refreshes the header of one.
    public func describe(_ set: WordSet) throws {
        self.set = set
        try save()
    }

    /// Appends several words in one save. Importing a pack one `add` at a time would
    /// rewrite the whole file per word.
    func addMany(_ newWords: [Word]) throws {
        words.append(contentsOf: newWords)
        try save()
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

    /// Deletes every word, after copying the file aside.
    ///
    /// The copy is not optional politeness: this throws away review progress that can
    /// take months to build and that nothing else in the app can reconstruct. Each
    /// backup is stamped with the time rather than overwriting one fixed name - clearing
    /// an already-cleared store twice would otherwise replace the useful backup with a
    /// copy of nothing.
    ///
    /// - Returns: the backup's location, or nil when there was nothing to delete.
    @discardableResult
    public func removeAll(now: Date = Date()) throws -> URL? {
        guard !words.isEmpty else { return nil }

        let backup = backupURL(now: now)

        try FileManager.default.createDirectory(
            at: backup.deletingLastPathComponent(),
            withIntermediateDirectories: true
        )

        if FileManager.default.fileExists(atPath: backup.path(percentEncoded: false)) {
            try FileManager.default.removeItem(at: backup)
        }

        try FileManager.default.copyItem(at: fileURL, to: backup)

        words = []
        try save()

        return backup
    }

    private func backupURL(now: Date) -> URL {
        let stem = fileURL.deletingPathExtension().lastPathComponent

        let stamp = DateFormatter()
        stamp.dateFormat = "yyyyMMdd-HHmmss"
        stamp.timeZone = TimeZone(identifier: "UTC")
        stamp.locale = Locale(identifier: "en_US_POSIX")

        return fileURL
            .deletingLastPathComponent()
            .appending(path: WordingPaths.backupsFolderName, directoryHint: .isDirectory)
            .appending(path: "\(stem)-\(stamp.string(from: now)).json", directoryHint: .notDirectory)
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

        let payload = WordFile(version: WordFile.currentVersion, set: set, words: words)
        let data = try WordingJSON.encoder.encode(payload)

        // Write alongside, then swap - a crash mid-write leaves the original intact.
        let temporary = fileURL.appendingPathExtension("tmp")
        try data.write(to: temporary, options: .atomic)
        _ = try FileManager.default.replaceItemAt(fileURL, withItemAt: temporary)
    }
}
