using Microsoft.Extensions.Time.Testing;
using Wording.Core;
using Wording.Core.Learning;

namespace Wording.Core.Tests;

public class WordSelectorTests
{
    static readonly DateTimeOffset Teraz = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    static Word Slowko(string oryginal, DateTimeOffset? termin = null, bool juzPowtarzane = true) => new()
    {
        Original = oryginal,
        Translation = oryginal + "-pl",
        CreatedUtc = Teraz,
        Review = new ReviewState
        {
            DueUtc = termin ?? Teraz,
            LastReviewedUtc = juzPowtarzane ? Teraz.AddDays(-1) : null,
            Repetitions = juzPowtarzane ? 1 : 0,
        },
    };

    static WordSelector Selektor(int ziarno = 1234) =>
        new(new FakeTimeProvider(Teraz), new Random(ziarno));

    [Fact]
    public void PustaLista_ZwracaNull()
    {
        Assert.Null(Selektor().PickNext([]));
    }

    [Fact]
    public void JednoSlowko_ZwracaJe()
    {
        var slowo = Slowko("scope");

        Assert.Same(slowo, Selektor().PickNext([slowo]));
    }

    [Fact]
    public void ZawszeZwracaSlowoZPrzekazanejListy()
    {
        var lista = new[] { Slowko("a"), Slowko("b"), Slowko("c") };
        var selektor = Selektor();

        for (var i = 0; i < 500; i++)
        {
            Assert.Contains(selektor.PickNext(lista), lista);
        }
    }

    [Fact]
    public void NoweSlowko_MaWyzszaWageNizSlowkoWTerminie()
    {
        var nowe = Slowko("nowe", juzPowtarzane: false);
        var wTerminie = Slowko("wterminie", Teraz);

        Assert.True(WordSelector.Weight(nowe, Teraz) > WordSelector.Weight(wTerminie, Teraz));
    }

    [Fact]
    public void Waga_RosnieWrazZOpoznieniem()
    {
        var swieze = Slowko("swieze", Teraz);
        var opoznioneODzien = Slowko("dzien", Teraz.AddDays(-1));
        var opoznioneOTydzien = Slowko("tydzien", Teraz.AddDays(-7));

        var w1 = WordSelector.Weight(swieze, Teraz);
        var w2 = WordSelector.Weight(opoznioneODzien, Teraz);
        var w3 = WordSelector.Weight(opoznioneOTydzien, Teraz);

        Assert.True(w1 < w2);
        Assert.True(w2 < w3);
    }

    [Fact]
    public void Waga_JestOgraniczonaZGory()
    {
        // Jedno zapomniane slowko sprzed lat nie moze zdominowac calej rotacji.
        var sprzedRoku = Slowko("stare", Teraz.AddYears(-1));
        var sprzedDziesieciuLat = Slowko("bardzostare", Teraz.AddYears(-10));

        Assert.Equal(
            WordSelector.Weight(sprzedRoku, Teraz),
            WordSelector.Weight(sprzedDziesieciuLat, Teraz));
    }

    [Fact]
    public void SlowkoNiewymagalne_MaMalaAleNiezerowaWage()
    {
        var zaMiesiac = Slowko("znane", Teraz.AddDays(30));

        var waga = WordSelector.Weight(zaMiesiac, Teraz);

        Assert.True(waga > 0, "dobrze znane slowko nie moze wypasc z rotacji calkiem");
        Assert.True(waga < WordSelector.DueWeight);
    }

    [Fact]
    public void PrzeterminowaneJestLosowaneZnaczniCzesciejNizSwiezoPowtorzone()
    {
        var przeterminowane = Slowko("zapomniane", Teraz.AddDays(-10));
        var swiezoPowtorzone = Slowko("znane", Teraz.AddDays(30));
        var lista = new[] { przeterminowane, swiezoPowtorzone };
        var selektor = Selektor();

        var trafienia = 0;
        const int Prob = 2000;

        for (var i = 0; i < Prob; i++)
        {
            if (ReferenceEquals(selektor.PickNext(lista), przeterminowane))
            {
                trafienia++;
            }
        }

        // Wagi to ok. 11.0 vs 0.032, wiec przeterminowane powinno dostac
        // grubo ponad 90% pokazow. Luzny prog, zeby test nie byl kruchy.
        Assert.True(trafienia > Prob * 0.9, $"przeterminowane trafilo {trafienia}/{Prob} razy");
    }

    [Fact]
    public void PrzyRownychWagach_RozklladJestZblizonyDoJednostajnego()
    {
        var lista = new[] { Slowko("a"), Slowko("b"), Slowko("c") };
        var selektor = Selektor();
        var licznik = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0, ["c"] = 0 };

        for (var i = 0; i < 3000; i++)
        {
            licznik[selektor.PickNext(lista)!.Original]++;
        }

        foreach (var (slowo, ile) in licznik)
        {
            Assert.True(ile is > 800 and < 1200, $"{slowo} trafilo {ile} razy, oczekiwano ok. 1000");
        }
    }
}
