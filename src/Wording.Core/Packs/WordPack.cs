namespace Wording.Core.Packs;

/// <summary>
/// A shareable set of words, as published at a URL.
/// <para>
/// Deliberately not the shape of words.json. That file is personal state - identifiers
/// and review progress - while a pack is only content. If they were the same type, a
/// published pack would carry its author's review history, and importing it would either
/// overwrite the reader's progress or invent one for them.
/// </para>
/// </summary>
public sealed class WordPack
{
    /// <summary>Becomes the file name of the imported set, so it is checked by <see cref="PackSlug"/>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Shown before the import is confirmed, so the user knows what they are about to add.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<PackEntry> Words { get; set; } = [];
}

/// <summary>One word in a pack: no identifier, no dates, no review state.</summary>
public sealed class PackEntry
{
    public string Original { get; set; } = string.Empty;

    public string Translation { get; set; } = string.Empty;
}
