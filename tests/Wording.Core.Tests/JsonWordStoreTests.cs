using Wording.Core;
using Wording.Core.Learning;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

public class JsonWordStoreTests
{
    static readonly DateTimeOffset Teraz = Fixtures.Teraz;

    [Fact]
    public void BrakPliku_DajePustyMagazyn()
    {
        using var dir = new TempDirectory();

        Assert.Empty(new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetAll());
    }

    [Fact]
    public void Add_ZapisujeNaDyskIWidaToPoPonownymWczytaniu()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).Add("scope", "zakres");

        var word = Assert.Single(new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetAll());

        Assert.Equal("scope", word.Original);
        Assert.Equal("zakres", word.Translation);
    }

    [Fact]
    public void Add_NadajeUnikalneIdentyfikatory()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());

        var ids = Enumerable.Range(0, 100)
            .Select(i => store.Add("slowo" + i, "tlumaczenie" + i).Id)
            .ToHashSet();

        Assert.Equal(100, ids.Count);
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Fact]
    public void Add_UstawiaSlowkoJakoWymagalneOdRazu()
    {
        using var dir = new TempDirectory();

        var word = new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).Add("scope", "zakres");

        Assert.Equal(Teraz, word.CreatedUtc);
        Assert.True(word.IsDue(Teraz));
        Assert.True(word.IsNew);
    }

    [Fact]
    public void Remove_UsuwaSlowkoTrwale()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        var word = store.Add("scope", "zakres");
        store.Add("cater", "zaspokoic");

        Assert.True(store.Remove(word.Id));

        var remaining = new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetAll();
        Assert.Equal("cater", Assert.Single(remaining).Original);
    }

    [Fact]
    public void Remove_NieistniejaceId_ZwracaFalse()
    {
        using var dir = new TempDirectory();

        Assert.False(new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).Remove(Guid.NewGuid()));
    }

    [Fact]
    public void Update_UtrwalaStanPowtorek()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        var word = store.Add("scope", "zakres");

        word.Review = SpacedRepetitionScheduler.Apply(word.Review, ReviewGrade.Good, Teraz);
        Assert.True(store.Update(word));

        var reloaded = new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetById(word.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded.Review.Repetitions);
        Assert.Equal(Teraz.AddDays(1), reloaded.Review.DueUtc);
    }

    [Fact]
    public void Zapis_NieZostawiaPlikuTymczasowego()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).Add("scope", "zakres");

        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp"));
    }

    [Fact]
    public void Reload_OdrzucaStanZPamieciNaRzeczDysku()
    {
        using var dir = new TempDirectory();
        var first = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        var second = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());

        second.Add("nimble", "zwinny");
        Assert.Empty(first.GetAll());

        first.Reload();

        Assert.Single(first.GetAll());
    }

    [Fact]
    public void ImportLegacyIfEmpty_PrzenosiSlowkaZeStaregoXml()
    {
        using var dir = new TempDirectory();
        dir.WriteLegacyXml(
            (1, "scope", "zakres"),
            (2, "cater", "zaspokoic"),
            (5, "efficient", "wydajny"));

        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        Assert.Equal(3, store.ImportLegacyIfEmpty(dir.XmlFile));

        Assert.Contains(store.GetAll(), w => w.Original == "efficient" && w.Translation == "wydajny");
        Assert.True(File.Exists(dir.JsonFile), "import powinien od razu zapisac plik JSON");
    }

    [Fact]
    public void ImportLegacyIfEmpty_NadajeNoweGuidyZamiastStarychLiczb()
    {
        using var dir = new TempDirectory();
        dir.WriteLegacyXml((1, "scope", "zakres"), (2, "cater", "zaspokoic"));

        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        store.ImportLegacyIfEmpty(dir.XmlFile);

        var ids = store.GetAll().Select(w => w.Id).ToHashSet();
        Assert.Equal(2, ids.Count);
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Fact]
    public void ImportLegacyIfEmpty_NieDotykaMagazynuKtoryJuzCosZawiera()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());
        store.Add("juz-tu-bylo", "istniejace");
        dir.WriteLegacyXml((1, "scope", "zakres"));

        Assert.Equal(0, store.ImportLegacyIfEmpty(dir.XmlFile));
        Assert.Equal("juz-tu-bylo", Assert.Single(store.GetAll()).Original);
    }

    [Fact]
    public void ImportLegacyIfEmpty_BezStaregoPliku_NicNieRobi()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());

        Assert.Equal(0, store.ImportLegacyIfEmpty(dir.XmlFile));
        Assert.Equal(0, store.ImportLegacyIfEmpty(null));
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Zapis_TworzyBrakujacyKatalog()
    {
        using var dir = new TempDirectory();
        var nested = Path.Combine(dir.Path, "a", "b", "words.json");

        new JsonWordStore(nested, Fixtures.Zegar()).Add("scope", "zakres");

        Assert.True(File.Exists(nested));
    }
}
