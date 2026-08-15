namespace Wording.Core.Packs;

/// <summary>Why a pack was rejected. The UI decides how to phrase each case.</summary>
public enum PackProblem
{
    /// <summary>The address was not https.</summary>
    NotHttps,

    /// <summary>The address could not be reached, or the server answered with an error.</summary>
    Network,

    /// <summary>Larger than <see cref="PackLimits.MaxPayloadBytes"/>, or too many words.</summary>
    TooLarge,

    /// <summary>Not JSON, or not the shape of a pack.</summary>
    Malformed,

    /// <summary>Parsed, but carried no usable word.</summary>
    Empty,

    /// <summary>The identifier could not be turned into a safe file name.</summary>
    UnsafeId,

    /// <summary>A set with this identifier is already on disk.</summary>
    AlreadyExists,
}

/// <summary>
/// Raised for every rejected pack. Carries <see cref="Problem"/> rather than a
/// user-facing sentence: Wording.Core has no opinion on wording.
/// </summary>
public sealed class WordPackException(PackProblem problem, string detail, Exception? inner = null)
    : Exception(detail, inner)
{
    public PackProblem Problem { get; } = problem;
}
