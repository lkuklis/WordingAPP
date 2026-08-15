import Foundation

/// What a pack holds, so the UI can label the two sides sensibly.
///
/// Kept as a string rather than an enum because it arrives inside a file downloaded from
/// an arbitrary URL: an unrecognised value has to fall back quietly, not fail the import.
/// A pack written by a newer version naming some third kind still reads as vocabulary
/// here, which is wrong in the labels and right in every way that matters.
///
/// A port of `Wording.Core.Packs.PackKind`.
public enum PackKind {
    /// A word and its translation. The default when nothing is declared.
    public static let vocabulary = "vocabulary"

    /// A term and a short definition or answer.
    public static let concepts = "concepts"

    public static func normalize(_ kind: String?) -> String {
        kind?.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() == concepts
            ? concepts
            : vocabulary
    }

    public static func isConcepts(_ kind: String?) -> Bool { normalize(kind) == concepts }
}
