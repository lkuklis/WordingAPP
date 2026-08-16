import Foundation

/// Lists the imported sets by reading the directory.
///
/// There is deliberately no registry file listing them. A registry has to be kept in
/// step with the disk and silently stops matching it the moment a file is moved or
/// deleted by hand; the directory cannot disagree with itself.
///
/// The user's own words.json is not included: it is not an import and has no header.
/// Naming it is the UI's job.
///
/// A port of `Wording.Core.Storage.WordSetCatalog`.
public enum WordSetCatalog {
    /// The file the app should open for a remembered set choice.
    ///
    /// Falls back to the user's own words whenever that choice cannot be honoured - no
    /// choice made, an identifier that is not a safe slug, or a set deleted from disk
    /// since it was chosen. Refusing to start because a remembered set has gone would
    /// leave the user with an app that will not open.
    public static func resolveActiveFile(
        _ setId: String?,
        dataFile: URL? = nil,
        setsDirectory: URL? = nil
    ) -> URL {
        let ownWords = dataFile ?? WordingPaths.dataFile()

        guard let slug = PackSlug.normalize(setId) else { return ownWords }

        let url = WordingPaths.setFile(slug, in: setsDirectory)

        return FileManager.default.fileExists(atPath: url.path(percentEncoded: false)) ? url : ownWords
    }

    public static func list(in setsDirectory: URL? = nil) -> [WordSetInfo] {
        let directory = setsDirectory ?? WordingPaths.setsDirectory()

        guard let entries = try? FileManager.default.contentsOfDirectory(
            at: directory,
            includingPropertiesForKeys: nil
        ) else {
            return []
        }

        return entries
            .filter { $0.pathExtension == "json" }
            .compactMap(read)
            .sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
    }

    /// Removes an imported set by moving its file into the backups directory.
    ///
    /// Moved rather than deleted: the file carries review progress that took time to
    /// build and that nothing can reconstruct. Same reasoning, and the same directory, as
    /// clearing every word from a store.
    ///
    /// - Returns: where the file went, or nil when there was no such set.
    @discardableResult
    public static func remove(
        _ id: String,
        in setsDirectory: URL? = nil,
        now: Date = Date()
    ) throws -> URL? {
        guard let slug = PackSlug.normalize(id) else { return nil }

        let directory = setsDirectory ?? WordingPaths.setsDirectory()
        let fileURL = WordingPaths.setFile(slug, in: directory)

        guard FileManager.default.fileExists(atPath: fileURL.path(percentEncoded: false)) else {
            return nil
        }

        let stamp = DateFormatter()
        stamp.dateFormat = "yyyyMMdd-HHmmss"
        stamp.timeZone = TimeZone(identifier: "UTC")
        stamp.locale = Locale(identifier: "en_US_POSIX")

        let backup = directory
            .appending(path: WordingPaths.backupsFolderName, directoryHint: .isDirectory)
            .appending(path: "\(slug)-\(stamp.string(from: now)).json", directoryHint: .notDirectory)

        try FileManager.default.createDirectory(
            at: backup.deletingLastPathComponent(),
            withIntermediateDirectories: true
        )

        if FileManager.default.fileExists(atPath: backup.path(percentEncoded: false)) {
            try FileManager.default.removeItem(at: backup)
        }

        try FileManager.default.moveItem(at: fileURL, to: backup)

        return backup
    }

    /// Reads one set, or nil when the file cannot be understood. A damaged file is left
    /// out of the list rather than taking the whole list down with it.
    public static func read(_ fileURL: URL) -> WordSetInfo? {
        guard let data = try? Data(contentsOf: fileURL),
            let file = try? WordingJSON.decoder.decode(WordFile.self, from: data)
        else {
            return nil
        }

        // The file name is the identity, not the header: they can disagree if the file
        // was renamed by hand, and the name on disk is the one that decides which file
        // a refresh or a delete would touch.
        let id = fileURL.deletingPathExtension().lastPathComponent
        let name = file.set?.name

        return WordSetInfo(
            id: id,
            name: (name?.isEmpty == false) ? name! : id,
            sourceUrl: file.set?.sourceUrl,
            kind: PackKind.normalize(file.set?.kind),
            wordCount: file.words.count,
            fileURL: fileURL
        )
    }
}
