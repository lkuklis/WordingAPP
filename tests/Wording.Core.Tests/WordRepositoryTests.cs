using System.Xml.Linq;
using Wording.Core;
using Wording.Core.Repository;

namespace Wording.Core.Tests;

/// <summary>
/// Tymczasowy plik XML w formacie, ktory czyta WordRepository.
/// </summary>
sealed class PlikTestowy : IDisposable
{
    public string Sciezka { get; }

    public PlikTestowy(params (int Id, string Original, string Translated)[] slowa)
    {
        Sciezka = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

        var dokument = new XDocument(
            new XElement("AllWords",
                slowa.Select(s => new XElement("Word",
                    new XElement("Id", s.Id),
                    new XElement("Original", s.Original),
                    new XElement("Translated", s.Translated)))));

        dokument.Save(Sciezka);
    }

    public XDocument Wczytaj() => XDocument.Load(Sciezka);

    public void Dispose()
    {
        if (File.Exists(Sciezka))
        {
            File.Delete(Sciezka);
        }
    }
}

public class WordRepositoryTests
{
    static PlikTestowy DomyslnyPlik() => new(
        (1, "scope", "zakres"),
        (2, "cater", "zaspokoic"),
        (5, "efficient", "wydajny"));

    [Fact]
    public void WczytujeWszystkieSlowaZPliku()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);

        var slowa = repo.GetAll().ToList();

        Assert.Equal(3, slowa.Count);
        Assert.Equal("scope", slowa[0].OriginalValue);
        Assert.Equal("zakres", slowa[0].TranslationValue);
    }

    [Fact]
    public void GetWordById_ZwracaWlasciweSlowo()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);

        var slowo = repo.GetWordById(2);

        Assert.NotNull(slowo);
        Assert.Equal("cater", slowo.OriginalValue);
    }

    [Fact]
    public void GetWordById_NieistniejaceId_ZwracaNull()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);

        Assert.Null(repo.GetWordById(999));
    }

    [Fact]
    public void AddWord_NadajeIdMaxPlusJeden()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);
        var nowe = new Word { OriginalValue = "nimble", TranslationValue = "zwinny" };

        repo.AddWord(nowe);

        // Najwyzsze istniejace Id to 5, wiec nowe dostaje 6 - dziury (3, 4) sa pomijane.
        Assert.Equal(6, nowe.Id);
    }

    [Fact]
    public void AddWord_ZapisujeDoPliku()
    {
        using var plik = DomyslnyPlik();
        new WordRepository(plik.Sciezka)
            .AddWord(new Word { OriginalValue = "nimble", TranslationValue = "zwinny" });

        // Nowa instancja czyta z dysku - potwierdza, ze zapis faktycznie sie odbyl.
        var poPonownymWczytaniu = new WordRepository(plik.Sciezka).GetAll().ToList();

        Assert.Equal(4, poPonownymWczytaniu.Count);
        Assert.Contains(poPonownymWczytaniu, w => w.OriginalValue == "nimble");
    }

    [Fact]
    public void DeleteWord_UsuwaSlowoZPliku()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);

        repo.DeleteWord(2);

        var pozostale = new WordRepository(plik.Sciezka).GetAll().ToList();
        Assert.Equal(2, pozostale.Count);
        Assert.DoesNotContain(pozostale, w => w.OriginalValue == "cater");
    }

    [Fact]
    public void EditWord_ZmieniaWartosciWPliku()
    {
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);

        repo.EditWord(new Word { Id = 1, OriginalValue = "scope", TranslationValue = "zasieg" });

        var poZmianie = new WordRepository(plik.Sciezka).GetWordById(1);
        Assert.NotNull(poZmianie);
        Assert.Equal("zasieg", poZmianie.TranslationValue);
    }

    [Fact]
    public void GetAll_NieWidziZmianBezRefreshData()
    {
        // Dokumentuje obecne zachowanie: repozytorium trzyma liste w pamieci,
        // wiec druga instancja piszaca do tego samego pliku jest niewidoczna
        // dopoki nie wywolamy RefreshData. To zrodlo rozjazdu miedzy oknem
        // glownym a dialogiem dodawania slowka.
        using var plik = DomyslnyPlik();
        var pierwsze = new WordRepository(plik.Sciezka);
        var drugie = new WordRepository(plik.Sciezka);

        drugie.AddWord(new Word { OriginalValue = "nimble", TranslationValue = "zwinny" });

        Assert.Equal(3, pierwsze.GetAll().Count());

        pierwsze.RefreshData();

        Assert.Equal(4, pierwsze.GetAll().Count());
    }

    [Fact]
    public void AddWord_PoUsunieciuNajwyzszegoId_UzywaIdPonownie()
    {
        // Dokumentuje pulapke obecnego schematu: Id = max + 1 liczone za kazdym
        // razem od nowa, wiec skasowanie ostatniego slowka zwalnia jego Id.
        // Dlatego przy synchronizacji miedzy urzadzeniami przejdziemy na GUID-y.
        using var plik = DomyslnyPlik();
        var repo = new WordRepository(plik.Sciezka);
        repo.DeleteWord(5);

        var nowe = new Word { OriginalValue = "nimble", TranslationValue = "zwinny" };
        repo.AddWord(nowe);

        Assert.Equal(3, nowe.Id);
    }
}
