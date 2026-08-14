using Microsoft.Extensions.Time.Testing;
using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class JsonWordStoreTests
{
    static readonly DateTimeOffset Teraz = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    static FakeTimeProvider Zegar() => new(Teraz);

    [Fact]
    public void BrakPliku_DajePustyMagazyn()
    {
        using var katalog = new TempKatalog();

        var magazyn = new JsonWordStore(katalog.PlikJson, Zegar());

        Assert.Empty(magazyn.GetAll());
    }

    [Fact]
    public void Add_ZapisujeNaDyskIWidaToPoPonownymWczytaniu()
    {
        using var katalog = new TempKatalog();
        new JsonWordStore(katalog.PlikJson, Zegar()).Add("scope", "zakres");

        var poWczytaniu = new JsonWordStore(katalog.PlikJson, Zegar()).GetAll();

        var slowo = Assert.Single(poWczytaniu);
        Assert.Equal("scope", slowo.Original);
        Assert.Equal("zakres", slowo.Translation);
    }

    [Fact]
    public void Add_NadajeUnikalneIdentyfikatory()
    {
        using var katalog = new TempKatalog();
        var magazyn = new JsonWordStore(katalog.PlikJson, Zegar());

        var identyfikatory = Enumerable.Range(0, 100)
            .Select(i => magazyn.Add("slowo" + i, "tlumaczenie" + i).Id)
            .ToHashSet();

        Assert.Equal(100, identyfikatory.Count);
        Assert.DoesNotContain(Guid.Empty, identyfikatory);
    }

    [Fact]
    public void Add_UstawiaSlowkoJakoWymagalneOdRazu()
    {
        using var katalog = new TempKatalog();

        var slowo = new JsonWordStore(katalog.PlikJson, Zegar()).Add("scope", "zakres");

        Assert.Equal(Teraz, slowo.CreatedUtc);
        Assert.Equal(Teraz, slowo.Review.DueUtc);
        Assert.Null(slowo.Review.LastReviewedUtc);
    }

    [Fact]
    public void Remove_UsuwaSlowkoTrwale()
    {
        using var katalog = new TempKatalog();
        var magazyn = new JsonWordStore(katalog.PlikJson, Zegar());
        var slowo = magazyn.Add("scope", "zakres");
        magazyn.Add("cater", "zaspokoic");

        Assert.True(magazyn.Remove(slowo.Id));

        var pozostale = new JsonWordStore(katalog.PlikJson, Zegar()).GetAll();
        Assert.Equal("cater", Assert.Single(pozostale).Original);
    }

    [Fact]
    public void Remove_NieistniejaceId_ZwracaFalse()
    {
        using var katalog = new TempKatalog();
        var magazyn = new JsonWordStore(katalog.PlikJson, Zegar());

        Assert.False(magazyn.Remove(Guid.NewGuid()));
    }

    [Fact]
    public void Update_UtrwalaStanPowtorek()
    {
        using var katalog = new TempKatalog();
        var magazyn = new JsonWordStore(katalog.PlikJson, Zegar());
        var slowo = magazyn.Add("scope", "zakres");

        slowo.Review = SpacedRepetitionScheduler.Apply(slowo.Review, ReviewGrade.Good, Teraz);
        Assert.True(magazyn.Update(slowo));

        var poWczytaniu = new JsonWordStore(katalog.PlikJson, Zegar()).GetById(slowo.Id);
        Assert.NotNull(poWczytaniu);
        Assert.Equal(1, poWczytaniu.Review.Repetitions);
        Assert.Equal(Teraz.AddDays(1), poWczytaniu.Review.DueUtc);
    }

    [Fact]
    public void Zapis_NieZostawiaPlikuTymczasowego()
    {
        using var katalog = new TempKatalog();
        new JsonWordStore(katalog.PlikJson, Zegar()).Add("scope", "zakres");

        Assert.Empty(Directory.GetFiles(katalog.Sciezka, "*.tmp"));
    }

    [Fact]
    public void Reload_OdrzucaStanZPamieciNaRzeczDysku()
    {
        using var katalog = new TempKatalog();
        var pierwszy = new JsonWordStore(katalog.PlikJson, Zegar());
        var drugi = new JsonWordStore(katalog.PlikJson, Zegar());

        drugi.Add("nimble", "zwinny");
        Assert.Empty(pierwszy.GetAll());

        pierwszy.Reload();

        Assert.Single(pierwszy.GetAll());
    }

    [Fact]
    public void OpenOrMigrate_PrzenosiSlowkaZeStaregoXml()
    {
        using var katalog = new TempKatalog();
        katalog.ZapiszStaryXml(
            (1, "scope", "zakres"),
            (2, "cater", "zaspokoic"),
            (5, "efficient", "wydajny"));

        var magazyn = JsonWordStore.OpenOrMigrate(katalog.PlikJson, katalog.PlikXml, Zegar());

        Assert.Equal(3, magazyn.GetAll().Count);
        Assert.Contains(magazyn.GetAll(), w => w.Original == "efficient" && w.Translation == "wydajny");
        Assert.True(File.Exists(katalog.PlikJson), "migracja powinna od razu zapisac plik JSON");
    }

    [Fact]
    public void OpenOrMigrate_NadajeNoweGuidyZamiastStarychLiczb()
    {
        using var katalog = new TempKatalog();
        katalog.ZapiszStaryXml((1, "scope", "zakres"), (2, "cater", "zaspokoic"));

        var magazyn = JsonWordStore.OpenOrMigrate(katalog.PlikJson, katalog.PlikXml, Zegar());

        var identyfikatory = magazyn.GetAll().Select(w => w.Id).ToHashSet();
        Assert.Equal(2, identyfikatory.Count);
        Assert.DoesNotContain(Guid.Empty, identyfikatory);
    }

    [Fact]
    public void OpenOrMigrate_NieNadpisujeIstniejacegoJson()
    {
        using var katalog = new TempKatalog();
        new JsonWordStore(katalog.PlikJson, Zegar()).Add("juz-tu-bylo", "istniejace");
        katalog.ZapiszStaryXml((1, "scope", "zakres"));

        var magazyn = JsonWordStore.OpenOrMigrate(katalog.PlikJson, katalog.PlikXml, Zegar());

        Assert.Equal("juz-tu-bylo", Assert.Single(magazyn.GetAll()).Original);
    }

    [Fact]
    public void OpenOrMigrate_BezStaregoPliku_DajePustyMagazyn()
    {
        using var katalog = new TempKatalog();

        var magazyn = JsonWordStore.OpenOrMigrate(katalog.PlikJson, katalog.PlikXml, Zegar());

        Assert.Empty(magazyn.GetAll());
    }

    [Fact]
    public void Zapis_TworzyBrakujacyKatalog()
    {
        using var katalog = new TempKatalog();
        var zagniezdzony = Path.Combine(katalog.Sciezka, "a", "b", "words.json");

        new JsonWordStore(zagniezdzony, Zegar()).Add("scope", "zakres");

        Assert.True(File.Exists(zagniezdzony));
    }
}
