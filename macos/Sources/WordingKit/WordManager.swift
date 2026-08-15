import Foundation

public enum WordingError: Error, Equatable {
    case emptyOriginal
    case emptyTranslation
}

/// The façade the UI talks to. A port of `Wording.Core.WordManager`.
public final class WordManager {
    let store: WordStore

    public init(store: WordStore) {
        self.store = store
    }

    public var words: [Word] { store.words }

    /// - Throws: `WordingError` when either side is empty.
    @discardableResult
    public func addWord(original: String, translation: String, now: Date = Date()) throws -> Word {
        let trimmedOriginal = original.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedTranslation = translation.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !trimmedOriginal.isEmpty else { throw WordingError.emptyOriginal }
        guard !trimmedTranslation.isEmpty else { throw WordingError.emptyTranslation }

        return try store.add(original: trimmedOriginal, translation: trimmedTranslation, now: now)
    }

    @discardableResult
    public func removeWord(id: UUID) throws -> Bool {
        try store.remove(id: id)
    }

    /// Deletes every word, keeping a copy of the file first.
    /// - Returns: the backup's location, or nil when there was nothing to delete.
    @discardableResult
    public func removeAllWords(now: Date = Date()) throws -> URL? {
        try store.removeAll(now: now)
    }

    /// The word that should go into the notification now.
    public func nextWordToShow(now: Date = Date()) -> Word? {
        WordSelector.pickNext(from: store.words, now: now)
    }

    /// Records a review grade and recomputes when the word is due next.
    /// - Returns: false when no word with that id exists any more.
    @discardableResult
    public func grade(id: UUID, grade: ReviewGrade, now: Date = Date()) throws -> Bool {
        guard var word = store.word(id: id) else { return false }

        word.review = SpacedRepetitionScheduler.apply(word.review, grade: grade, now: now)

        return try store.update(word)
    }
}
