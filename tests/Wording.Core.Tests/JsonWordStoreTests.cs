using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class JsonWordStoreTests
{
    static readonly DateTimeOffset Now = Fixtures.Now;

    [Fact]
    public void MissingFile_GivesAnEmptyStore()
    {
        using var dir = new TempDirectory();

        Assert.Empty(new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetAll());
    }

    [Fact]
    public void Add_WritesToDiskAndSurvivesAReload()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Clock()).Add("scope", "zakres");

        var word = Assert.Single(new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetAll());

        Assert.Equal("scope", word.Original);
        Assert.Equal("zakres", word.Translation);
    }

    [Fact]
    public void Add_AssignsUniqueIdentifiers()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        var ids = Enumerable.Range(0, 100)
            .Select(i => store.Add("word" + i, "translation" + i).Id)
            .ToHashSet();

        Assert.Equal(100, ids.Count);
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Fact]
    public void Add_MakesTheWordDueImmediately()
    {
        using var dir = new TempDirectory();

        var word = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).Add("scope", "zakres");

        Assert.Equal(Now, word.CreatedUtc);
        Assert.True(word.IsDue(Now));
        Assert.True(word.IsNew);
    }

    [Fact]
    public void Remove_DeletesTheWordPermanently()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        var word = store.Add("scope", "zakres");
        store.Add("cater", "zaspokoic");

        Assert.True(store.Remove(word.Id));

        var remaining = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetAll();
        Assert.Equal("cater", Assert.Single(remaining).Original);
    }

    [Fact]
    public void Remove_UnknownId_ReturnsFalse()
    {
        using var dir = new TempDirectory();

        Assert.False(new JsonWordStore(dir.JsonFile, Fixtures.Clock()).Remove(Guid.NewGuid()));
    }

    [Fact]
    public void Update_PersistsReviewState()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        var word = store.Add("scope", "zakres");

        word.Review = SpacedRepetitionScheduler.Apply(word.Review, ReviewGrade.Good, Now);
        Assert.True(store.Update(word));

        var reloaded = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetById(word.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded.Review.Repetitions);
        Assert.Equal(Now.AddDays(1), reloaded.Review.DueUtc);
    }

    [Fact]
    public void Save_LeavesNoTemporaryFileBehind()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Clock()).Add("scope", "zakres");

        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp"));
    }

    [Fact]
    public void Reload_DiscardsInMemoryStateInFavourOfDisk()
    {
        using var dir = new TempDirectory();
        var first = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        var second = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        second.Add("nimble", "zwinny");
        Assert.Empty(first.GetAll());

        first.Reload();

        Assert.Single(first.GetAll());
    }

    [Fact]
    public void ImportLegacyIfEmpty_BringsWordsOverFromTheOldXml()
    {
        using var dir = new TempDirectory();
        dir.WriteLegacyXml(
            (1, "scope", "zakres"),
            (2, "cater", "zaspokoic"),
            (5, "efficient", "wydajny"));

        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        Assert.Equal(3, store.ImportLegacyIfEmpty(dir.XmlFile));

        Assert.Contains(store.GetAll(), w => w.Original == "efficient" && w.Translation == "wydajny");
        Assert.True(File.Exists(dir.JsonFile), "the import should write the JSON file straight away");
    }

    [Fact]
    public void ImportLegacyIfEmpty_KeepsNonAsciiCharactersThroughJson()
    {
        using var dir = new TempDirectory();
        dir.WriteLegacyXml((1, "default", "domyślnie"), (2, "suppress", "tłumić"));

        new JsonWordStore(dir.JsonFile, Fixtures.Clock()).ImportLegacyIfEmpty(dir.XmlFile);

        // Re-read from disk - this exercises the whole XML to JSON to file round trip.
        var reloaded = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetAll();

        Assert.Contains(reloaded, w => w.Translation == "domyślnie");
        Assert.Contains(reloaded, w => w.Translation == "tłumić");
    }

    [Fact]
    public void ImportLegacyIfEmpty_AssignsFreshGuidsInsteadOfOldNumbers()
    {
        using var dir = new TempDirectory();
        dir.WriteLegacyXml((1, "scope", "zakres"), (2, "cater", "zaspokoic"));

        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.ImportLegacyIfEmpty(dir.XmlFile);

        var ids = store.GetAll().Select(w => w.Id).ToHashSet();
        Assert.Equal(2, ids.Count);
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Fact]
    public void ImportLegacyIfEmpty_LeavesANonEmptyStoreAlone()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Add("already-here", "existing");
        dir.WriteLegacyXml((1, "scope", "zakres"));

        Assert.Equal(0, store.ImportLegacyIfEmpty(dir.XmlFile));
        Assert.Equal("already-here", Assert.Single(store.GetAll()).Original);
    }

    [Fact]
    public void ImportLegacyIfEmpty_WithNoLegacyFile_DoesNothing()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        Assert.Equal(0, store.ImportLegacyIfEmpty(dir.XmlFile));
        Assert.Equal(0, store.ImportLegacyIfEmpty(null));
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Set_IsNullForTheUsersOwnWords()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Add("mine", "moje");

        Assert.Null(store.Set);
        Assert.DoesNotContain("\"set\"", File.ReadAllText(dir.JsonFile), StringComparison.Ordinal);
    }

    [Fact]
    public void Set_SurvivesEveryLaterSave()
    {
        // Grading a word rewrites the whole file; dropping the header there would lose
        // the set's name and the address it can be refreshed from.
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Describe(new WordSet { Id = "travel-basics", Name = "Travel basics", SourceUrl = "https://example.com/p.json" });

        var word = store.Add("airport", "aeropuerto");
        word.Review = Wording.Core.Learning.ReviewState.New(Fixtures.Now);
        store.Update(word);

        var reloaded = new JsonWordStore(dir.JsonFile).Set;

        Assert.NotNull(reloaded);
        Assert.Equal("travel-basics", reloaded.Id);
        Assert.Equal("https://example.com/p.json", reloaded.SourceUrl);
    }

    [Fact]
    public void RemoveAll_EmptiesTheStoreAndKeepsACopy()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Add("scope", "zakres");
        store.Add("cater", "zaspokoić");

        var backup = store.RemoveAll();

        Assert.Empty(store.GetAll());
        Assert.Empty(new JsonWordStore(dir.JsonFile).GetAll());

        Assert.NotNull(backup);
        Assert.True(File.Exists(backup));

        // The copy is only worth taking if it still holds what was deleted.
        var saved = new JsonWordStore(backup).GetAll();
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, word => word.Original == "scope");
    }

    [Fact]
    public void RemoveAll_OnAnEmptyStoreDoesNothingAndBacksUpNothing()
    {
        // Otherwise clearing twice would replace the useful backup with a copy of nothing.
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        Assert.Null(store.RemoveAll());
        Assert.False(Directory.Exists(Path.Combine(dir.Path, WordingPaths.BackupsFolderName)));
    }

    [Fact]
    public void RemoveAll_PutsTheBackupWhereTheSetCatalogueWillNotSeeIt()
    {
        // A backup written beside a set would otherwise be listed as a set of its own.
        using var dir = new TempDirectory();
        Directory.CreateDirectory(dir.SetsDirectory);

        var store = new JsonWordStore(dir.SetFile("travel-basics"), Fixtures.Clock());
        store.Describe(new WordSet { Id = "travel-basics", Name = "Travel basics" });
        store.Add("airport", "aeropuerto");

        Assert.NotNull(store.RemoveAll());

        var listed = WordSetCatalog.List(dir.SetsDirectory);
        Assert.Equal("travel-basics", Assert.Single(listed).Id);
    }

    [Fact]
    public void RemoveAll_KeepsTheSetHeader()
    {
        // Emptying a set does not stop it being that set - the name and source stay.
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Describe(new WordSet { Id = "travel-basics", Name = "Travel basics" });
        store.Add("airport", "aeropuerto");

        store.RemoveAll();

        Assert.Equal("Travel basics", new JsonWordStore(dir.JsonFile).Set?.Name);
    }

    [Fact]
    public void Save_CreatesTheMissingDirectory()
    {
        using var dir = new TempDirectory();
        var nested = Path.Combine(dir.Path, "a", "b", "words.json");

        new JsonWordStore(nested, Fixtures.Clock()).Add("scope", "zakres");

        Assert.True(File.Exists(nested));
    }
}
