using Wording.Core.Packs;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordSetCatalogTests
{
    static readonly Uri Source = new("https://example.com/pack.json");

    [Fact]
    public void List_IsEmptyBeforeAnythingIsImported()
    {
        using var dir = new TempDirectory();

        Assert.Empty(WordSetCatalog.List(dir.SetsDirectory));
    }

    [Fact]
    public void List_ReportsTheNameFromTheHeaderAndTheCountFromTheWords()
    {
        using var dir = new TempDirectory();
        Import(dir, "travel-basics", "Travel basics", ("airport", "aeropuerto"), ("ticket", "billete"));

        var set = Assert.Single(WordSetCatalog.List(dir.SetsDirectory));

        Assert.Equal("travel-basics", set.Id);
        Assert.Equal("Travel basics", set.Name);
        Assert.Equal(2, set.WordCount);
        Assert.Equal(Source.ToString(), set.SourceUrl);
    }

    [Fact]
    public void List_CountsWhatIsInTheFileRatherThanWhatWasImported()
    {
        // A stored count would start lying the moment a word is deleted.
        using var dir = new TempDirectory();
        Import(dir, "travel-basics", "Travel basics", ("airport", "aeropuerto"), ("ticket", "billete"));

        var store = new JsonWordStore(dir.SetFile("travel-basics"));
        store.Remove(store.GetAll()[0].Id);

        Assert.Equal(1, Assert.Single(WordSetCatalog.List(dir.SetsDirectory)).WordCount);
    }

    [Fact]
    public void List_SkipsAFileItCannotUnderstandInsteadOfFailing()
    {
        using var dir = new TempDirectory();
        Import(dir, "good", "Good one", ("airport", "aeropuerto"));
        File.WriteAllText(Path.Combine(dir.SetsDirectory, "broken.json"), "{ not json");

        Assert.Equal("good", Assert.Single(WordSetCatalog.List(dir.SetsDirectory)).Id);
    }

    [Fact]
    public void List_TakesTheIdentifierFromTheFileNameNotTheHeader()
    {
        // They disagree once a file is renamed by hand, and the name on disk is the one
        // that decides which file a refresh would touch.
        using var dir = new TempDirectory();
        Import(dir, "travel-basics", "Travel basics", ("airport", "aeropuerto"));

        File.Move(dir.SetFile("travel-basics"), dir.SetFile("renamed"));

        Assert.Equal("renamed", Assert.Single(WordSetCatalog.List(dir.SetsDirectory)).Id);
    }

    [Fact]
    public void List_IgnoresTheUsersOwnWordsFile()
    {
        // words.json is not an import and lives outside the sets directory.
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Clock()).Add("mine", "moje");
        Import(dir, "travel-basics", "Travel basics", ("airport", "aeropuerto"));

        Assert.Equal("travel-basics", Assert.Single(WordSetCatalog.List(dir.SetsDirectory)).Id);
    }

    static void Import(TempDirectory dir, string id, string name, params (string, string)[] words) =>
        new WordPackImporter(dir.SetsDirectory, Fixtures.Clock()).Import(
            new WordPack
            {
                Id = id,
                Name = name,
                Words = [.. words.Select(word => new PackEntry { Original = word.Item1, Translation = word.Item2 })],
            },
            Source);
}
