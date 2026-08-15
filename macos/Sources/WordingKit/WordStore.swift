import Foundation

/// Magazyn slowek w pliku JSON.
///
/// Port `Wording.Core.Storage.JsonWordStore`. Zapis jest atomowy (plik
/// tymczasowy + podmiana), zeby przerwanie procesu nie zostawilo uciętego
/// pliku z danymi uzytkownika.
public final class WordStore {
    public let fileURL: URL
    public private(set) var words: [Word] = []

    public init(fileURL: URL = WordingPaths.dataFile()) throws {
        self.fileURL = fileURL
        try reload()
    }

    /// Wczytuje ponownie z dysku, odrzucajac stan z pamieci.
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

    /// Zapisuje zmiany w slowku juz obecnym w magazynie (np. po ocenie powtorki).
    @discardableResult
    public func update(_ word: Word) throws -> Bool {
        guard let index = words.firstIndex(where: { $0.id == word.id }) else {
            return false
        }

        words[index] = word
        try save()

        return true
    }

    /// Dopisuje slowka jednym zapisem - uzywane przy zasiewie pakietu startowego.
    func append(_ newWords: [Word]) throws {
        words.append(contentsOf: newWords)
        try save()
    }

    private func save() throws {
        let directory = fileURL.deletingLastPathComponent()
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)

        let payload = WordFile(version: WordFile.currentVersion, words: words)
        let data = try WordingJSON.encoder.encode(payload)

        // Zapis obok, potem podmiana - w razie awarii oryginal zostaje nietkniety.
        let temporary = fileURL.appendingPathExtension("tmp")
        try data.write(to: temporary, options: .atomic)
        _ = try FileManager.default.replaceItemAt(fileURL, withItemAt: temporary)
    }
}
