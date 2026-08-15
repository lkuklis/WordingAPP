namespace Wording.Core.Packs;

/// <summary>
/// The catalogue of packs published alongside the app, so the user can pick one from a
/// list instead of pasting an address.
/// <para>
/// It exists because a directory cannot be listed over plain HTTP. Everywhere else in
/// this app a listing beats a registry - the set catalogue reads the directory precisely
/// so it cannot disagree with the disk - but there is no remote equivalent, so the index
/// is written down and a test keeps it in step with the files.
/// </para>
/// </summary>
public sealed class PackIndex
{
    public int Version { get; set; } = 1;

    public List<PackIndexEntry> Packs { get; set; } = [];
}

/// <summary>
/// One row in the catalogue: enough to show the user what a pack is before downloading it.
/// <para>
/// Deliberately carries no address and no file name. The app builds the URL itself from
/// <see cref="Id"/> and the address the index was fetched from, so a file downloaded from
/// the internet cannot point the app at somewhere else. It also makes the catalogue work
/// unchanged in a fork or a mirror, because every address is relative to the index.
/// </para>
/// </summary>
public sealed class PackIndexEntry
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>See <see cref="PackKind"/>. Absent means vocabulary.</summary>
    public string? Kind { get; set; }

    /// <summary>Shown in the list. The pack itself decides what actually gets imported.</summary>
    public int WordCount { get; set; }
}
