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
    public void Save_CreatesTheMissingDirectory()
    {
        using var dir = new TempDirectory();
        var nested = Path.Combine(dir.Path, "a", "b", "words.json");

        new JsonWordStore(nested, Fixtures.Clock()).Add("scope", "zakres");

        Assert.True(File.Exists(nested));
    }
}
