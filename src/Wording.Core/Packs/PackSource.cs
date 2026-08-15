namespace Wording.Core.Packs;

/// <summary>
/// Where the published catalogue lives, and how a pack's address is derived from it.
/// </summary>
public static class PackSource
{
    /// <summary>
    /// The catalogue in this repository, on the default branch. Pointing at a branch
    /// rather than a tag is deliberate: a pack added to the repository then shows up in
    /// versions of the app that are already installed, so the catalogue grows without a
    /// release. The other half of that bargain is that a broken index breaks the browse
    /// window for everyone at once, which is why CI validates it.
    /// </summary>
    public const string OfficialIndexUrl =
        "https://raw.githubusercontent.com/lkuklis/WordingAPP/master/learning_data/index.json";

    /// <summary>
    /// The address of a pack listed in an index.
    /// <para>
    /// Built from the identifier and the index's own address, never from anything the
    /// index says. That is what stops a downloaded catalogue from sending the app
    /// somewhere else, and it is why the same file works in a fork without editing.
    /// </para>
    /// </summary>
    public static Uri PackUrl(Uri indexUrl, string id)
    {
        ArgumentNullException.ThrowIfNull(indexUrl);

        return new Uri(indexUrl, PackSlug.Require(id) + ".json");
    }
}
