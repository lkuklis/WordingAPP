using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordManagerTests
{
    static readonly DateTimeOffset Now = Fixtures.Now;

    static WordManager Build(TempDirectory dir) =>
        new(new JsonWordStore(dir.JsonFile, Fixtures.Clock()), Fixtures.Clock(), new Random(1234));

    [Fact]
    public void SharedStore_BothScreensSeeTheSameDataWithoutReloading()
    {
        // Fix for a pre-migration bug: the main window and the add dialog had separate
        // repositories, so a new word only appeared after a manual reload from disk.
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());

        var mainWindow = new WordManager(store, Fixtures.Clock());
        var addDialog = new WordManager(store, Fixtures.Clock());

        addDialog.AddWord("nimble", "zwinny");

        Assert.Contains(mainWindow.GetWords(), w => w.Original == "nimble");
    }

    [Fact]
    public void AddWord_RejectsAnEmptyWord()
    {
        using var dir = new TempDirectory();

        Assert.Throws<ArgumentException>(() => Build(dir).AddWord("   ", "zakres"));
    }

    [Fact]
    public void AddWord_RejectsAnEmptyTranslation()
    {
        using var dir = new TempDirectory();

        Assert.Throws<ArgumentException>(() => Build(dir).AddWord("scope", ""));
    }

    [Fact]
    public void AddWord_TrimsWhitespace()
    {
        using var dir = new TempDirectory();

        var word = Build(dir).AddWord("  scope  ", "\tzakres\n");

        Assert.Equal("scope", word.Original);
        Assert.Equal("zakres", word.Translation);
    }

    [Fact]
    public void Grade_RecomputesTheDueDateAndPersistsIt()
    {
        using var dir = new TempDirectory();
        var manager = Build(dir);
        var word = manager.AddWord("scope", "zakres");

        Assert.True(manager.Grade(word.Id, ReviewGrade.Good));

        var fromDisk = new JsonWordStore(dir.JsonFile, Fixtures.Clock()).GetById(word.Id);
        Assert.NotNull(fromDisk);
        Assert.Equal(1, fromDisk.Review.Repetitions);
        Assert.Equal(Now.AddDays(1), fromDisk.Review.DueUtc);
    }

    [Fact]
    public void Grade_UnknownId_ReturnsFalse()
    {
        using var dir = new TempDirectory();

        Assert.False(Build(dir).Grade(Guid.NewGuid(), ReviewGrade.Good));
    }

    [Fact]
    public void NextWordToShow_EmptyList_ReturnsNull()
    {
        using var dir = new TempDirectory();

        Assert.Null(Build(dir).NextWordToShow());
    }

    [Fact]
    public void NextWordToShow_ReturnsAWordFromTheList()
    {
        using var dir = new TempDirectory();
        var manager = Build(dir);
        manager.AddWord("scope", "zakres");
        manager.AddWord("cater", "zaspokoic");

        var shown = manager.NextWordToShow();

        Assert.NotNull(shown);
        Assert.Contains(manager.GetWords(), w => w.Id == shown.Id);
    }

    [Fact]
    public void WordsGradedAsKnown_StopDominatingTheRotation()
    {
        // The whole point of the mechanism: what we grade as known should show up less.
        using var dir = new TempDirectory();
        var manager = Build(dir);
        var known = manager.AddWord("known", "znane");
        var unknown = manager.AddWord("unknown", "nieznane");

        // The known word passes a few successful reviews, so its due date moves away.
        for (var i = 0; i < 3; i++)
        {
            manager.Grade(known.Id, ReviewGrade.Good);
        }

        var hits = 0;
        const int Attempts = 1000;

        for (var i = 0; i < Attempts; i++)
        {
            if (manager.NextWordToShow()!.Id == unknown.Id)
            {
                hits++;
            }
        }

        Assert.True(hits > Attempts * 0.8, $"the unknown word was picked only {hits}/{Attempts} times");
    }

    [Fact]
    public void RemoveWord_DeletesTheWordAndReturnsFalseForAnUnknownId()
    {
        using var dir = new TempDirectory();
        var manager = Build(dir);
        var word = manager.AddWord("scope", "zakres");

        Assert.True(manager.RemoveWord(word.Id));
        Assert.Empty(manager.GetWords());
        Assert.False(manager.RemoveWord(word.Id));
    }
}
