namespace Wording.Core.Packs;

/// <summary>
/// Bounds every downloaded pack has to fit in.
/// <para>
/// A pack comes from a URL the user pasted, so it is untrusted input: without caps a
/// single bad address could exhaust memory, write a data file too large to load, or
/// produce a "word" long enough to break the notification and the grid. The Swift port
/// carries the same numbers - see WordingKit/PackLimits.swift.
/// </para>
/// </summary>
public static class PackLimits
{
    /// <summary>Largest response accepted, before parsing.</summary>
    public const int MaxPayloadBytes = 2 * 1024 * 1024;

    public const int MaxWords = 5_000;

    /// <summary>Longest word or translation. Notifications truncate long text anyway.</summary>
    public const int MaxFieldLength = 200;

    public const int MaxNameLength = 80;

    public const int MaxDescriptionLength = 300;

    public const int MaxIdLength = 64;

    /// <summary>Rows kept from the published catalogue. The payload cap alone would
    /// allow tens of thousands of tiny entries, which is a list nobody can use.</summary>
    public const int MaxIndexEntries = 500;

    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(30);
}
