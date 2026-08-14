using Microsoft.Extensions.Time.Testing;
using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class WordManagerTests
{
    static readonly DateTimeOffset Teraz = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    static (WordManager Manager, JsonWordStore Magazyn, FakeTimeProvider Zegar) Zbuduj(TempKatalog katalog)
    {
        var zegar = new FakeTimeProvider(Teraz);
        var magazyn = new JsonWordStore(katalog.PlikJson, zegar);

        return (new WordManager(magazyn, zegar, new Random(1234)), magazyn, zegar);
    }

    [Fact]
    public void WspolnyMagazyn_ObaEkranyWidzaTeSameDaneBezOdswiezania()
    {
        // To jest naprawa bledu przypietego w kroku 1: wczesniej okno glowne
        // i okienko dodawania mialy osobne repozytoria, wiec nowe slowko
        // pojawialo sie dopiero po recznym przeladowaniu z dysku.
        using var katalog = new TempKatalog();
        var zegar = new FakeTimeProvider(Teraz);
        var wspolnyMagazyn = new JsonWordStore(katalog.PlikJson, zegar);

        var oknoGlowne = new WordManager(wspolnyMagazyn, zegar);
        var okienkoDodawania = new WordManager(wspolnyMagazyn, zegar);

        okienkoDodawania.AddWord("nimble", "zwinny");

        Assert.Contains(oknoGlowne.GetWords(), w => w.Original == "nimble");
    }

    [Fact]
    public void AddWord_OdrzucaPusteSlowko()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);

        Assert.Throws<ArgumentException>(() => manager.AddWord("   ", "zakres"));
    }

    [Fact]
    public void AddWord_OdrzucaPusteTlumaczenie()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);

        Assert.Throws<ArgumentException>(() => manager.AddWord("scope", ""));
    }

    [Fact]
    public void AddWord_PrzycinaBialeZnaki()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);

        var slowo = manager.AddWord("  scope  ", "\tzakres\n");

        Assert.Equal("scope", slowo.Original);
        Assert.Equal("zakres", slowo.Translation);
    }

    [Fact]
    public void Grade_PrzeliczaTerminIUtrwalaGoNaDysku()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);
        var slowo = manager.AddWord("scope", "zakres");

        Assert.True(manager.Grade(slowo.Id, ReviewGrade.Good));

        var zDysku = new JsonWordStore(katalog.PlikJson, new FakeTimeProvider(Teraz)).GetById(slowo.Id);
        Assert.NotNull(zDysku);
        Assert.Equal(1, zDysku.Review.Repetitions);
        Assert.Equal(Teraz.AddDays(1), zDysku.Review.DueUtc);
    }

    [Fact]
    public void Grade_NieistniejaceId_ZwracaFalse()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);

        Assert.False(manager.Grade(Guid.NewGuid(), ReviewGrade.Good));
    }

    [Fact]
    public void NextWordToShow_PustaLista_ZwracaNull()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);

        Assert.Null(manager.NextWordToShow());
    }

    [Fact]
    public void NextWordToShow_ZwracaSlowkoZListy()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);
        manager.AddWord("scope", "zakres");
        manager.AddWord("cater", "zaspokoic");

        var pokazane = manager.NextWordToShow();

        Assert.NotNull(pokazane);
        Assert.Contains(manager.GetWords(), w => w.Id == pokazane.Id);
    }

    [Fact]
    public void OcenioneJakoZnane_PrzestajeDominowacWRotacji()
    {
        // Sedno calego kroku: to, co oceniamy jako znane, ma sie pokazywac rzadziej.
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);
        var znane = manager.AddWord("znane", "known");
        var nieznane = manager.AddWord("nieznane", "unknown");

        // Znane przechodzi kilka udanych powtorek, wiec jego termin ucieka w przyszlosc.
        for (var i = 0; i < 3; i++)
        {
            manager.Grade(znane.Id, ReviewGrade.Good);
        }

        var trafieniaNieznanego = 0;
        const int Prob = 1000;

        for (var i = 0; i < Prob; i++)
        {
            if (manager.NextWordToShow()!.Id == nieznane.Id)
            {
                trafieniaNieznanego++;
            }
        }

        Assert.True(
            trafieniaNieznanego > Prob * 0.8,
            $"nieznane slowko trafilo tylko {trafieniaNieznanego}/{Prob} razy");
    }

    [Fact]
    public void RemoveWord_UsuwaSlowkoIZwracaFalseDlaNieistniejacego()
    {
        using var katalog = new TempKatalog();
        var (manager, _, _) = Zbuduj(katalog);
        var slowo = manager.AddWord("scope", "zakres");

        Assert.True(manager.RemoveWord(slowo.Id));
        Assert.Empty(manager.GetWords());
        Assert.False(manager.RemoveWord(slowo.Id));
    }
}
