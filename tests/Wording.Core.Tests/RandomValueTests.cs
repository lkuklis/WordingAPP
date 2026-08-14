using Wording.Core;

namespace Wording.Core.Tests;

public class RandomValueTests
{
    [Fact]
    public void GetRandom_PustaLista_ZwracaDefault()
    {
        var puste = new List<Word>();

        Assert.Null(puste.GetRandom());
    }

    [Fact]
    public void GetRandom_PustaListaTypuWartosciowego_ZwracaZero()
    {
        var puste = new List<int>();

        Assert.Equal(0, puste.GetRandom());
    }

    [Fact]
    public void GetRandom_JedenElement_ZwracaTenElement()
    {
        var jeden = new List<string> { "scope" };

        Assert.Equal("scope", jeden.GetRandom());
    }

    [Fact]
    public void GetRandom_ZawszeZwracaElementZListy()
    {
        var lista = new List<int> { 10, 20, 30, 40, 50 };

        for (var i = 0; i < 500; i++)
        {
            Assert.Contains(lista.GetRandom(), lista);
        }
    }

    [Fact]
    public void GetRandom_TrafiaWKazdyElementListy()
    {
        // Losowanie jest jednostajne - przy 1000 probach na 3 elementach
        // kazdy powinien paść co najmniej raz. Ten test pilnuje, ze nie
        // zwracamy w kolko tego samego indeksu.
        var lista = new List<int> { 1, 2, 3 };
        var trafione = new HashSet<int>();

        for (var i = 0; i < 1000; i++)
        {
            trafione.Add(lista.GetRandom());
        }

        Assert.Equal(3, trafione.Count);
    }
}
