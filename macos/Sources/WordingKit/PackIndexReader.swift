import Foundation

/// Parses the published catalogue.
///
/// A single unusable row is dropped rather than refused. The catalogue is the only way
/// most people will ever find a pack, so one malformed entry must not hide every good one
/// behind an error - unlike a pack itself, where a rejected file simply is not imported
/// and the user loses nothing.
///
/// A port of `Wording.Core.Packs.PackIndexReader`.
public enum PackIndexReader {
    public static func read(_ payload: Data) throws -> [PackIndexEntry] {
        guard payload.count <= PackLimits.maxPayloadBytes else { throw WordPackError.tooLarge }

        let index: PackIndex

        do {
            index = try JSONDecoder().decode(PackIndex.self, from: payload)
        } catch {
            throw WordPackError.malformed("the catalogue is not valid JSON")
        }

        return clean(index.packs)
    }

    /// Applies every rule, dropping the rows that cannot be shown or fetched.
    public static func clean(_ entries: [PackIndexEntry]) -> [PackIndexEntry] {
        var cleaned: [PackIndexEntry] = []
        var seen = Set<String>()

        for entry in entries {
            // The identifier decides the address the app will fetch, so it gets exactly
            // the same treatment as one inside a pack.
            guard let slug = PackSlug.normalize(entry.id), seen.insert(slug).inserted else { continue }

            let name = PackText.clean(entry.name)

            guard !name.isEmpty else { continue }

            let description = PackText.truncate(
                PackText.clean(entry.description ?? ""),
                to: PackLimits.maxDescriptionLength
            )

            cleaned.append(
                PackIndexEntry(
                    id: slug,
                    name: PackText.truncate(name, to: PackLimits.maxNameLength),
                    description: description.isEmpty ? nil : description,
                    kind: PackKind.normalize(entry.kind),
                    wordCount: min(max(entry.wordCount, 0), PackLimits.maxWords)
                )
            )

            if cleaned.count == PackLimits.maxIndexEntries { break }
        }

        return cleaned
    }
}
