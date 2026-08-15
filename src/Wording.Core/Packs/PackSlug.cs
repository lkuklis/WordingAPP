namespace Wording.Core.Packs;

/// <summary>
/// Turns a pack identifier into something safe to use as a file name.
/// <para>
/// This is a security boundary, not tidiness. The identifier arrives inside a file
/// downloaded from an arbitrary URL and decides which file gets written, so an id of
/// "../words.json" would overwrite exactly the data the feature exists to protect.
/// The rule is therefore an allow-list: anything not matching is refused, never
/// "cleaned up" - silently rewriting an id would let two different packs collapse onto
/// one file.
/// </para>
/// </summary>
public static class PackSlug
{
    /// <summary>
    /// Names Windows refuses to use for a file, whatever the extension. They are all
    /// letters and digits, so the character rule alone would let them through.
    /// </summary>
    static readonly HashSet<string> ReservedOnWindows = new(StringComparer.Ordinal)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
    };

    /// <summary>
    /// Accepts an identifier, lower-cased. Case is the only difference tolerated;
    /// everything else has to be a lower-case letter, a digit or a hyphen.
    /// </summary>
    public static bool TryNormalize(string? id, out string slug)
    {
        slug = string.Empty;

        if (string.IsNullOrEmpty(id) || id.Length > PackLimits.MaxIdLength)
        {
            return false;
        }

        var candidate = id.ToLowerInvariant();

        foreach (var character in candidate)
        {
            var allowed = character is >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '-';

            if (!allowed)
            {
                return false;
            }
        }

        // A leading or trailing hyphen makes for awkward file names and lets two ids
        // differ by something invisible in a list.
        if (candidate[0] == '-' || candidate[^1] == '-')
        {
            return false;
        }

        if (ReservedOnWindows.Contains(candidate))
        {
            return false;
        }

        slug = candidate;
        return true;
    }

    /// <summary>Same rule, as a guard that throws the shared pack error.</summary>
    public static string Require(string? id)
    {
        if (!TryNormalize(id, out var slug))
        {
            throw new WordPackException(
                PackProblem.UnsafeId,
                $"'{id}' is not a usable pack id: expected 1-{PackLimits.MaxIdLength} characters of a-z, 0-9 or '-'");
        }

        return slug;
    }
}
