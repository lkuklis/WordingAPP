import Foundation

/// Parses a downloaded pack and decides whether it is fit to import.
///
/// Every check happens here, before anything reaches the disk: the caller gets either a
/// pack that is known to be safe to write, or a `WordPackError`.
///
/// Structural problems are refused. Two display-only fields - the name and the
/// description - are truncated instead, because they cannot harm anything and failing a
/// whole pack over a long title would leave the user with no way forward: they did not
/// write the file and cannot fix it.
///
/// A port of `Wording.Core.Packs.WordPackReader`.
public enum WordPackReader {
    public static func read(_ payload: Data) throws -> WordPack {
        guard payload.count <= PackLimits.maxPayloadBytes else { throw WordPackError.tooLarge }

        let pack: WordPack

        do {
            pack = try JSONDecoder().decode(WordPack.self, from: payload)
        } catch {
            throw WordPackError.malformed("the pack is not valid JSON")
        }

        return try validate(pack)
    }

    /// Applies every rule to an already-parsed pack. Split out so the repository's own
    /// packs can be checked without going through a download.
    public static func validate(_ pack: WordPack) throws -> WordPack {
        let slug = try PackSlug.require(pack.id)
        let name = clean(pack.name)

        guard !name.isEmpty else { throw WordPackError.malformed("the pack has no name") }
        guard pack.words.count <= PackLimits.maxWords else { throw WordPackError.tooLarge }

        var entries: [PackEntry] = []
        entries.reserveCapacity(pack.words.count)

        for entry in pack.words {
            let original = clean(entry.original)
            let translation = clean(entry.translation)

            // A blank line in a hand-edited pack is noise, not a reason to refuse the
            // rest of it.
            if original.isEmpty || translation.isEmpty { continue }

            // A field this long is not a word - it is a sign the file is some other
            // format that happens to parse. Truncating would silently change meaning.
            guard original.count <= PackLimits.maxFieldLength,
                translation.count <= PackLimits.maxFieldLength
            else {
                throw WordPackError.malformed(
                    "a word exceeds the \(PackLimits.maxFieldLength) character limit")
            }

            entries.append(PackEntry(original: original, translation: translation))
        }

        guard !entries.isEmpty else { throw WordPackError.empty }

        let description = truncate(clean(pack.description ?? ""), to: PackLimits.maxDescriptionLength)

        return WordPack(
            id: slug,
            name: truncate(name, to: PackLimits.maxNameLength),
            description: description.isEmpty ? nil : description,
            words: entries
        )
    }

    /// Trims, and folds every control character - newlines and tabs included - into a
    /// space. They would otherwise reach a notification body and a list row.
    private static func clean(_ value: String) -> String {
        String(value.map { $0.isNewline || $0.unicodeScalars.allSatisfy(CharacterSet.controlCharacters.contains) ? " " : $0 })
            .trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private static func truncate(_ value: String, to limit: Int) -> String {
        guard value.count > limit else { return value }

        return String(value.prefix(limit)).trimmingCharacters(in: .whitespaces)
    }
}
