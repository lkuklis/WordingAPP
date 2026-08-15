import Foundation

public enum WordingError: Error, Equatable {
    case emptyOriginal
    case emptyTranslation
}

/// Fasada dla warstwy UI. Port `Wording.Core.WordManager`.
public final class WordManager {
    let store: WordStore

    public init(store: WordStore) {
        self.store = store
    }

    public var words: [Word] { store.words }

    /// - Throws: `WordingError` gdy ktorakolwiek ze stron jest pusta.
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

    /// Slowko, ktore powinno teraz trafic do powiadomienia.
    public func nextWordToShow(now: Date = Date()) -> Word? {
        WordSelector.pickNext(from: store.words, now: now)
    }

    /// Zapisuje ocene powtorki i przelicza termin nastepnego pokazania.
    /// - Returns: false, gdy slowka o takim id juz nie ma.
    @discardableResult
    public func grade(id: UUID, grade: ReviewGrade, now: Date = Date()) throws -> Bool {
        guard var word = store.word(id: id) else { return false }

        word.review = SpacedRepetitionScheduler.apply(word.review, grade: grade, now: now)

        return try store.update(word)
    }
}
