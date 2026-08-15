using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordManagerTests
{
    static readonly DateTimeOffset Teraz = Fixtures.Teraz;

    static WordManager Zbuduj(TempDirectory dir) =>
        new(new JsonWordStore(dir.JsonFile, Fixtures.Zegar()), Fixtures.Zegar(), new Random(1234));

    [Fact]
    public void WspolnyMagazyn_ObaEkranyWidzaTeSameDaneBezOdswiezania()
    {
        // Naprawa bledu z wersji sprzed migracji: okno glowne i okienko dodawania
        // mialy osobne repozytoria, wiec nowe slowko pojawialo sie dopiero po
        // recznym przeladowaniu z dysku.
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());

        var mainWindow = new WordManager(store, Fixtures.Zegar());
        var addDialog = new WordManager(store, Fixtures.Zegar());

        addDialog.AddWord("nimble", "zwinny");

        Assert.Contains(mainWindow.GetWords(), w => w.Original == "nimble");
    }

    [Fact]
    public void AddWord_OdrzucaPusteSlowko()
    {
        using var dir = new TempDirectory();

        Assert.Throws<ArgumentException>(() => Zbuduj(dir).AddWord("   ", "zakres"));
    }

    [Fact]
    public void AddWord_OdrzucaPusteTlumaczenie()
    {
        using var dir = new TempDirectory();

        Assert.Throws<ArgumentException>(() => Zbuduj(dir).AddWord("scope", ""));
    }

    [Fact]
    public void AddWord_PrzycinaBialeZnaki()
    {
        using var dir = new TempDirectory();

        var word = Zbuduj(dir).AddWord("  scope  ", "\tzakres\n");

        Assert.Equal("scope", word.Original);
        Assert.Equal("zakres", word.Translation);
    }

    [Fact]
    public void Grade_PrzeliczaTerminIUtrwalaGoNaDysku()
    {
        using var dir = new TempDirectory();
        var manager = Zbuduj(dir);
        var word = manager.AddWord("scope", "zakres");

        Assert.True(manager.Grade(word.Id, ReviewGrade.Good));

        var fromDisk = new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetById(word.Id);
        Assert.NotNull(fromDisk);
        Assert.Equal(1, fromDisk.Review.Repetitions);
        Assert.Equal(Teraz.AddDays(1), fromDisk.Review.DueUtc);
    }

    [Fact]
    public void Grade_NieistniejaceId_ZwracaFalse()
    {
        using var dir = new TempDirectory();

        Assert.False(Zbuduj(dir).Grade(Guid.NewGuid(), ReviewGrade.Good));
    }

    [Fact]
    public void NextWordToShow_PustaLista_ZwracaNull()
    {
        using var dir = new TempDirectory();

        Assert.Null(Zbuduj(dir).NextWordToShow());
    }

    [Fact]
    public void NextWordToShow_ZwracaSlowkoZListy()
    {
        using var dir = new TempDirectory();
        var manager = Zbuduj(dir);
        manager.AddWord("scope", "zakres");
        manager.AddWord("cater", "zaspokoic");

        var shown = manager.NextWordToShow();

        Assert.NotNull(shown);
        Assert.Contains(manager.GetWords(), w => w.Id == shown.Id);
    }

    [Fact]
    public void OcenioneJakoZnane_PrzestajeDominowacWRotacji()
    {
        // Sedno calego mechanizmu: to, co oceniamy jako znane, ma sie pokazywac rzadziej.
        using var dir = new TempDirectory();
        var manager = Zbuduj(dir);
        var known = manager.AddWord("znane", "known");
        var unknown = manager.AddWord("nieznane", "unknown");

        // Znane przechodzi kilka udanych powtorek, wiec jego termin ucieka w przyszlosc.
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

        Assert.True(hits > Attempts * 0.8, $"nieznane slowko trafilo tylko {hits}/{Attempts} razy");
    }

    [Fact]
    public void RemoveWord_UsuwaSlowkoIZwracaFalseDlaNieistniejacego()
    {
        using var dir = new TempDirectory();
        var manager = Zbuduj(dir);
        var word = manager.AddWord("scope", "zakres");

        Assert.True(manager.RemoveWord(word.Id));
        Assert.Empty(manager.GetWords());
        Assert.False(manager.RemoveWord(word.Id));
    }
}
