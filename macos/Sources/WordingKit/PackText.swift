import Foundation

/// Tidying shared by the pack reader and the catalogue reader. Both take text from a file
/// downloaded off the internet and put it in a notification and a list, so both need the
/// same two guarantees: nothing invisible, and nothing endless.
///
/// A port of `Wording.Core.Packs.Text`.
enum PackText {
    /// Trims, and folds every control character - newlines and tabs included - into a
    /// space. They would otherwise reach a notification body and a list row.
    static func clean(_ value: String) -> String {
        String(
            value.map {
                $0.isNewline || $0.unicodeScalars.allSatisfy(CharacterSet.controlCharacters.contains)
                    ? " " : $0
            }
        )
        .trimmingCharacters(in: .whitespacesAndNewlines)
    }

    static func truncate(_ value: String, to limit: Int) -> String {
        guard value.count > limit else { return value }

        return String(value.prefix(limit)).trimmingCharacters(in: .whitespaces)
    }
}
