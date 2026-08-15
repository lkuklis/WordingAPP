import Foundation

/// Bounds every downloaded pack has to fit in.
///
/// A pack comes from a URL the user pasted, so it is untrusted input: without caps a
/// single bad address could exhaust memory, write a data file too large to load, or
/// produce a "word" long enough to break the notification and the list. The .NET port
/// carries the same numbers - see Wording.Core/Packs/PackLimits.cs.
public enum PackLimits {
    /// Largest response accepted, before parsing.
    public static let maxPayloadBytes = 2 * 1024 * 1024

    public static let maxWords = 5_000

    /// Longest word or translation. Notifications truncate long text anyway.
    public static let maxFieldLength = 200

    public static let maxNameLength = 80

    public static let maxDescriptionLength = 300

    public static let maxIdLength = 64

    public static let downloadTimeout: TimeInterval = 30
}
