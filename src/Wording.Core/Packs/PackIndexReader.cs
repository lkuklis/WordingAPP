using System.Text.Json;
using Wording.Core.Storage;

namespace Wording.Core.Packs;

/// <summary>
/// Parses the published catalogue.
/// <para>
/// A single unusable row is dropped rather than refused. The catalogue is the only way
/// most people will ever find a pack, so one malformed entry must not hide every good one
/// behind an error - unlike a pack itself, where a rejected file simply is not imported
/// and the user loses nothing.
/// </para>
/// </summary>
public static class PackIndexReader
{
    public static IReadOnlyList<PackIndexEntry> Read(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > PackLimits.MaxPayloadBytes)
        {
            throw new WordPackException(
                PackProblem.TooLarge,
                $"the catalogue is {payload.Length} bytes, the limit is {PackLimits.MaxPayloadBytes}");
        }

        PackIndex? index;

        try
        {
            index = JsonSerializer.Deserialize(payload, WordJsonContext.Default.PackIndex);
        }
        catch (JsonException error)
        {
            throw new WordPackException(PackProblem.Malformed, "the catalogue is not valid JSON", error);
        }

        if (index is null)
        {
            throw new WordPackException(PackProblem.Malformed, "the catalogue is empty");
        }

        return Clean(index.Packs);
    }

    /// <summary>Applies every rule, dropping the rows that cannot be shown or fetched.</summary>
    public static IReadOnlyList<PackIndexEntry> Clean(IEnumerable<PackIndexEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var cleaned = new List<PackIndexEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            // The identifier decides the address the app will fetch, so it gets exactly
            // the same treatment as one inside a pack.
            if (!PackSlug.TryNormalize(entry.Id, out var slug) || !seen.Add(slug))
            {
                continue;
            }

            var name = Text.Clean(entry.Name);

            if (name.Length == 0)
            {
                continue;
            }

            cleaned.Add(new PackIndexEntry
            {
                Id = slug,
                Name = Text.Truncate(name, PackLimits.MaxNameLength),
                Description = Text.Truncate(Text.Clean(entry.Description ?? string.Empty), PackLimits.MaxDescriptionLength)
                    is { Length: > 0 } text
                    ? text
                    : null,
                Kind = PackKind.Normalize(entry.Kind),
                WordCount = Math.Clamp(entry.WordCount, 0, PackLimits.MaxWords),
            });

            if (cleaned.Count == PackLimits.MaxIndexEntries)
            {
                break;
            }
        }

        return cleaned;
    }
}
