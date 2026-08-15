using Wording.Core;
using Wording.Core.Learning;

namespace Wording.Core.Tests;

public class WordSelectorTests
{
    static readonly DateTimeOffset Teraz = Fixtures.Teraz;

    static Word Slowko(string original, DateTimeOffset? due = null, bool reviewed = true) => new()
    {
        Original = original,
        Translation = original + "-pl",
        CreatedUtc = Teraz,
        Review = new ReviewState
        {
            DueUtc = due ?? Teraz,
            LastReviewedUtc = reviewed ? Teraz.AddDays(-1) : null,
            Repetitions = reviewed ? 1 : 0,
        },
    };

    [Fact]
    public void PustaLista_ZwracaNull()
    {
        Assert.Null(WordSelector.PickNext([], Teraz, new Random(1234)));
    }

    [Fact]
    public void JednoSlowko_ZwracaJe()
    {
        var word = Slowko("scope");

        Assert.Same(word, WordSelector.PickNext([word], Teraz, new Random(1234)));
    }

    [Fact]
    public void ZawszeZwracaSlowoZPrzekazanejListy()
    {
        var words = new[] { Slowko("a"), Slowko("b"), Slowko("c") };
        var random = new Random(1234);

        for (var i = 0; i < 500; i++)
        {
            Assert.Contains(WordSelector.PickNext(words, Teraz, random), words);
        }
    }

    [Fact]
    public void NoweSlowko_MaWyzszaWageNizSlowkoWTerminie()
    {
        var fresh = Slowko("nowe", reviewed: false);
        var due = Slowko("wterminie", Teraz);

        Assert.True(WordSelector.Weight(fresh, Teraz) > WordSelector.Weight(due, Teraz));
    }

    [Fact]
    public void Waga_RosnieWrazZOpoznieniem()
    {
        var onTime = WordSelector.Weight(Slowko("swieze", Teraz), Teraz);
        var oneDay = WordSelector.Weight(Slowko("dzien", Teraz.AddDays(-1)), Teraz);
        var oneWeek = WordSelector.Weight(Slowko("tydzien", Teraz.AddDays(-7)), Teraz);

        Assert.True(onTime < oneDay);
        Assert.True(oneDay < oneWeek);
    }

    [Fact]
    public void Waga_JestOgraniczonaZGory()
    {
        // Jedno zapomniane slowko sprzed lat nie moze zdominowac calej rotacji.
        var year = WordSelector.Weight(Slowko("stare", Teraz.AddYears(-1)), Teraz);
        var decade = WordSelector.Weight(Slowko("bardzostare", Teraz.AddYears(-10)), Teraz);

        Assert.Equal(year, decade);
    }

    [Fact]
    public void SlowkoNiewymagalne_MaMalaAleNiezerowaWage()
    {
        var weight = WordSelector.Weight(Slowko("znane", Teraz.AddDays(30)), Teraz);

        Assert.True(weight > 0, "dobrze znane slowko nie moze wypasc z rotacji calkiem");
        Assert.True(weight < WordSelector.DueWeight);
    }

    [Fact]
    public void PrzeterminowaneJestLosowaneZnaczniCzesciejNizSwiezoPowtorzone()
    {
        var overdue = Slowko("zapomniane", Teraz.AddDays(-10));
        var known = Slowko("znane", Teraz.AddDays(30));
        var words = new[] { overdue, known };
        var random = new Random(1234);

        var hits = 0;
        const int Attempts = 2000;

        for (var i = 0; i < Attempts; i++)
        {
            if (ReferenceEquals(WordSelector.PickNext(words, Teraz, random), overdue))
            {
                hits++;
            }
        }

        // Wagi to ok. 11.0 vs 0.032, wiec przeterminowane powinno dostac
        // grubo ponad 90% pokazow. Luzny prog, zeby test nie byl kruchy.
        Assert.True(hits > Attempts * 0.9, $"przeterminowane trafilo {hits}/{Attempts} razy");
    }

    [Fact]
    public void PrzyRownychWagach_RozkladJestZblizonyDoJednostajnego()
    {
        var words = new[] { Slowko("a"), Slowko("b"), Slowko("c") };
        var random = new Random(1234);
        var counts = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0, ["c"] = 0 };

        for (var i = 0; i < 3000; i++)
        {
            counts[WordSelector.PickNext(words, Teraz, random)!.Original]++;
        }

        foreach (var (word, count) in counts)
        {
            Assert.True(count is > 800 and < 1200, $"{word} trafilo {count} razy, oczekiwano ok. 1000");
        }
    }
}
