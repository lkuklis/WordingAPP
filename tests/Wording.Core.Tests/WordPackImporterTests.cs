using Wording.Core.Learning;
using Wording.Core.Packs;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordPackImporterTests
{
    static readonly Uri Source = new("https://example.com/travel-basics.json");

    static WordPack Pack(params (string Original, string Translation)[] words) => new()
    {
        Id = "travel-basics",
        Name = "Travel basics",
        Words = [.. words.Select(word => new PackEntry
        {
            Original = word.Original,
            Translation = word.Translation,
        })],
    };

    [Fact]
    public void Import_WritesTheSetToItsOwnFile()
    {
        using var dir = new TempDirectory();

        var result = Importer(dir).Import(Pack(("airport", "aeropuerto")), Source);

        Assert.True(File.Exists(dir.SetFile("travel-basics")));
        Assert.Equal("travel-basics", result.Set.Id);
        Assert.Equal("Travel basics", result.Set.Name);
        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void Import_LeavesTheUsersOwnWordsUntouched()
    {
        // The whole point of the feature: importing must not disturb what is open.
        using var dir = new TempDirectory();
        var own = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        own.Add("mine", "moje");

        var before = File.ReadAllText(dir.JsonFile);

        Importer(dir).Import(Pack(("airport", "aeropuerto")), Source);

        Assert.Equal(before, File.ReadAllText(dir.JsonFile));
        Assert.Equal("mine", Assert.Single(new JsonWordStore(dir.JsonFile).GetAll()).Original);
    }

    [Fact]
    public void Import_RecordsTheHeaderSoTheSetCanBeRefreshedLater()
    {
        using var dir = new TempDirectory();

        Importer(dir).Import(Pack(("airport", "aeropuerto")), Source);

        var set = new JsonWordStore(dir.SetFile("travel-basics")).Set;

        Assert.NotNull(set);
        Assert.Equal("travel-basics", set.Id);
        Assert.Equal(Source.ToString(), set.SourceUrl);
        Assert.Equal(Fixtures.Now, set.ImportedUtc);
    }

    [Fact]
    public void Import_ImportedWordsStartNewAndDue()
    {
        using var dir = new TempDirectory();

        Importer(dir).Import(Pack(("airport", "aeropuerto")), Source);

        var word = Assert.Single(new JsonWordStore(dir.SetFile("travel-basics")).GetAll());

        Assert.True(word.IsNew);
        Assert.True(word.IsDue(Fixtures.Now));
    }

    [Fact]
    public void Import_RefusesToOverwriteASetAlreadyOnDisk()
    {
        using var dir = new TempDirectory();
        var importer = Importer(dir);
        importer.Import(Pack(("airport", "aeropuerto")), Source);

        var error = Assert.Throws<WordPackException>(() => importer.Import(Pack(("other", "inne")), Source));

        Assert.Equal(PackProblem.AlreadyExists, error.Problem);

        // And the refused import changed nothing.
        Assert.Equal("airport", Assert.Single(new JsonWordStore(dir.SetFile("travel-basics")).GetAll()).Original);
    }

    [Fact]
    public void Import_ReplacingMergesInsteadOfStartingOver()
    {
        using var dir = new TempDirectory();
        var importer = Importer(dir);
        importer.Import(Pack(("airport", "aeropuerto")), Source);

        var result = importer.Import(
            Pack(("airport", "aeropuerto"), ("ticket", "billete")),
            Source,
            replaceExisting: true);

        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(2, new JsonWordStore(dir.SetFile("travel-basics")).GetAll().Count);
    }

    [Fact]
    public void Import_ReplacingKeepsTheReviewProgressOfWordsAlreadyThere()
    {
        // The one thing an import must never do is undo someone's learning.
        using var dir = new TempDirectory();
        var importer = Importer(dir);
        importer.Import(Pack(("airport", "aeropuerto")), Source);

        var store = new JsonWordStore(dir.SetFile("travel-basics"), Fixtures.Clock());
        var id = store.GetAll()[0].Id;

        Assert.True(new WordManager(store, Fixtures.Clock()).Grade(id, ReviewGrade.Good));

        var graded = store.GetById(id)!;

        importer.Import(Pack(("airport", "aeropuerto"), ("ticket", "billete")), Source, replaceExisting: true);

        var reloaded = new JsonWordStore(dir.SetFile("travel-basics")).GetById(id);

        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsNew);
        Assert.Equal(graded.Review.Repetitions, reloaded.Review.Repetitions);
        Assert.Equal(graded.Review.DueUtc, reloaded.Review.DueUtc);
    }

    [Theory]
    [InlineData("Airport", "AEROPUERTO")]
    [InlineData("  airport  ", " aeropuerto ")]
    public void Import_TreatsTheSameWordAsAlreadyPresentWhateverTheCaseOrSpacing(string original, string translation)
    {
        using var dir = new TempDirectory();
        var importer = Importer(dir);
        importer.Import(Pack(("airport", "aeropuerto")), Source);

        var result = importer.Import(Pack((original, translation)), Source, replaceExisting: true);

        Assert.Equal(0, result.Added);
        Assert.Single(new JsonWordStore(dir.SetFile("travel-basics")).GetAll());
    }

    [Fact]
    public void Import_CountsAWordRepeatedInsideThePackOnlyOnce()
    {
        using var dir = new TempDirectory();

        var result = Importer(dir).Import(
            Pack(("airport", "aeropuerto"), ("airport", "aeropuerto")),
            Source);

        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void Import_RefusesAnIdentifierThatWouldEscapeTheSetsDirectory()
    {
        using var dir = new TempDirectory();
        var pack = Pack(("airport", "aeropuerto"));
        pack.Id = "../words";

        Assert.Equal(
            PackProblem.UnsafeId,
            Assert.Throws<WordPackException>(() => Importer(dir).Import(pack, Source)).Problem);

        Assert.False(File.Exists(dir.JsonFile));
    }

    [Fact]
    public void Import_WithoutASourceUrlStillWorks()
    {
        // A pack opened from a local file has no address to record.
        using var dir = new TempDirectory();

        Importer(dir).Import(Pack(("airport", "aeropuerto")), source: null);

        Assert.Null(new JsonWordStore(dir.SetFile("travel-basics")).Set!.SourceUrl);
    }

    static WordPackImporter Importer(TempDirectory dir) => new(dir.SetsDirectory, Fixtures.Clock());
}
