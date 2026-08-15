namespace Wording.Core.Packs;

/// <summary>
/// What a pack holds, so the UI can label the two sides sensibly.
/// <para>
/// Kept as a string rather than an enum because it arrives inside a file downloaded from
/// an arbitrary URL: an unrecognised value has to fall back quietly, not fail the import.
/// A pack written by a newer version naming some third kind still reads as vocabulary
/// here, which is wrong in the labels and right in every way that matters.
/// </para>
/// </summary>
public static class PackKind
{
    /// <summary>A word and its translation. The default when nothing is declared.</summary>
    public const string Vocabulary = "vocabulary";

    /// <summary>A term and a short definition or answer.</summary>
    public const string Concepts = "concepts";

    public static string Normalize(string? kind) =>
        string.Equals(kind?.Trim(), Concepts, StringComparison.OrdinalIgnoreCase)
            ? Concepts
            : Vocabulary;

    public static bool IsConcepts(string? kind) => Normalize(kind) == Concepts;
}
