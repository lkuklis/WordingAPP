using System.Text.Json;
using Wording.Core.Storage;

namespace Wording.Core.Packs;

/// <summary>
/// Parses a downloaded pack and decides whether it is fit to import.
/// <para>
/// Every check happens here, before anything reaches the disk: the caller gets either a
/// pack that is known to be safe to write, or a <see cref="WordPackException"/>.
/// </para>
/// <para>
/// Structural problems are refused. Two display-only fields - the name and the
/// description - are truncated instead, because they cannot harm anything and failing a
/// whole pack over a long title would leave the user with no way forward: they did not
/// write the file and cannot fix it.
/// </para>
/// </summary>
public static class WordPackReader
{
    public static WordPack Read(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > PackLimits.MaxPayloadBytes)
        {
            throw new WordPackException(
                PackProblem.TooLarge,
                $"the pack is {payload.Length} bytes, the limit is {PackLimits.MaxPayloadBytes}");
        }

        WordPack? pack;

        try
        {
            pack = JsonSerializer.Deserialize(payload, WordJsonContext.Default.WordPack);
        }
        catch (JsonException error)
        {
            throw new WordPackException(PackProblem.Malformed, "the pack is not valid JSON", error);
        }

        if (pack is null)
        {
            throw new WordPackException(PackProblem.Malformed, "the pack is empty");
        }

        return Validate(pack);
    }

    /// <summary>
    /// Applies every rule to an already-parsed pack. Split out so the repository's own
    /// packs can be checked without going through a download.
    /// </summary>
    public static WordPack Validate(WordPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        var slug = PackSlug.Require(pack.Id);
        var name = Text.Clean(pack.Name);

        if (name.Length == 0)
        {
            throw new WordPackException(PackProblem.Malformed, "the pack has no name");
        }

        if (pack.Words.Count > PackLimits.MaxWords)
        {
            throw new WordPackException(
                PackProblem.TooLarge,
                $"the pack has {pack.Words.Count} words, the limit is {PackLimits.MaxWords}");
        }

        var entries = new List<PackEntry>(pack.Words.Count);

        foreach (var entry in pack.Words)
        {
            var original = Text.Clean(entry.Original);
            var translation = Text.Clean(entry.Translation);

            // A blank line in a hand-edited pack is noise, not a reason to refuse the
            // rest of it.
            if (original.Length == 0 || translation.Length == 0)
            {
                continue;
            }

            // A field this long is not a word - it is a sign the file is some other
            // format that happens to parse. Truncating would silently change meaning.
            if (original.Length > PackLimits.MaxFieldLength || translation.Length > PackLimits.MaxFieldLength)
            {
                throw new WordPackException(
                    PackProblem.Malformed,
                    $"'{Preview(original)}' exceeds the {PackLimits.MaxFieldLength} character limit for a word");
            }

            entries.Add(new PackEntry { Original = original, Translation = translation });
        }

        if (entries.Count == 0)
        {
            throw new WordPackException(PackProblem.Empty, "the pack carries no usable word");
        }

        return new WordPack
        {
            Id = slug,
            Name = Text.Truncate(name, PackLimits.MaxNameLength),
            Kind = PackKind.Normalize(pack.Kind),
            Description = Text.Truncate(Text.Clean(pack.Description ?? string.Empty), PackLimits.MaxDescriptionLength) is { Length: > 0 } text
                ? text
                : null,
            Words = entries,
        };
    }

    static string Preview(string value) => Text.Truncate(value, 40);
}
