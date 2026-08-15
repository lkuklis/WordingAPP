import Foundation

/// Turns a pack identifier into something safe to use as a file name.
///
/// This is a security boundary, not tidiness. The identifier arrives inside a file
/// downloaded from an arbitrary URL and decides which file gets written, so an id of
/// "../words.json" would overwrite exactly the data the feature exists to protect.
/// The rule is therefore an allow-list: anything not matching is refused, never
/// "cleaned up" - silently rewriting an id would let two different packs collapse onto
/// one file.
///
/// A port of `Wording.Core.Packs.PackSlug`.
public enum PackSlug {
    /// Names Windows refuses to use for a file, whatever the extension. They are all
    /// letters and digits, so the character rule alone would let them through. Checked
    /// here too: the two apps share the pack format, so a pack accepted on macOS has to
    /// be one Windows could store as well.
    private static let reservedOnWindows: Set<String> = [
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    ]

    private static let allowed = Set("abcdefghijklmnopqrstuvwxyz0123456789-")

    /// Accepts an identifier, lower-cased. Case is the only difference tolerated;
    /// everything else has to be a lower-case letter, a digit or a hyphen.
    public static func normalize(_ id: String?) -> String? {
        guard let id, !id.isEmpty, id.count <= PackLimits.maxIdLength else { return nil }

        let candidate = id.lowercased()

        guard candidate.allSatisfy({ allowed.contains($0) }) else { return nil }

        // A leading or trailing hyphen makes for awkward file names and lets two ids
        // differ by something invisible in a list.
        guard candidate.first != "-", candidate.last != "-" else { return nil }
        guard !reservedOnWindows.contains(candidate) else { return nil }

        return candidate
    }

    /// Same rule, as a guard that throws the shared pack error.
    public static func require(_ id: String?) throws -> String {
        guard let slug = normalize(id) else { throw WordPackError.unsafeId }
        return slug
    }
}
