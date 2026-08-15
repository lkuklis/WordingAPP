using Wording.Core.Storage;

namespace Wording.Core.Tests;

/// <summary>
/// Runs the import against the real starter pack from the repository rather than
/// against synthetic data.
/// </summary>
public class StarterPackMigrationTests
{
    static string StarterPack =>
        Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName);

    [Fact]
    public void StarterPack_ShipsNextToTheBinary()
    {
        Assert.True(File.Exists(StarterPack), $"could not find {StarterPack}");
    }

    [Fact]
    public void StarterPack_ImportsCompletelyAndWithoutBlankEntries()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        Assert.Equal(38, store.ImportLegacyIfEmpty(StarterPack));

        var words = store.GetAll();

        Assert.All(words, word =>
        {
            Assert.NotEqual(Guid.Empty, word.Id);
            Assert.NotEmpty(word.Original);
            Assert.NotEmpty(word.Translation);
            Assert.True(word.IsNew);
            Assert.True(word.IsDue(Fixtures.Now));
        });

        Assert.Contains(words, w => w.Original == "scope" && w.Translation == "zakres");
        Assert.Equal(38, words.Select(w => w.Id).Distinct().Count());
    }

    [Fact]
    public void StarterPack_KeepsNonAsciiCharactersThroughJson()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Clock()).ImportLegacyIfEmpty(StarterPack);

        // Re-read from disk - this exercises the whole round trip.
        var reloaded = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetAll();

        Assert.Contains(reloaded, w => w.Translation.Contains("domyślnie"));
        Assert.Contains(reloaded, w => w.Translation.Contains("tłumić"));
    }
}
