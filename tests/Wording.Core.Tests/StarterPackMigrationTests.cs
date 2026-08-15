using Wording.Core.Storage;

namespace Wording.Core.Tests;

/// <summary>
/// Import na prawdziwym pakiecie startowym z repozytorium, nie na danych syntetycznych.
/// </summary>
public class StarterPackMigrationTests
{
    static string PakietStartowy =>
        Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName);

    [Fact]
    public void PakietStartowy_JestDostepnyObokBinarki()
    {
        Assert.True(File.Exists(PakietStartowy), $"nie znaleziono {PakietStartowy}");
    }

    [Fact]
    public void PakietStartowy_ImportujeSieWCalosciIBezPustychWpisow()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Zegar());

        Assert.Equal(38, store.ImportLegacyIfEmpty(PakietStartowy));

        var words = store.GetAll();

        Assert.All(words, word =>
        {
            Assert.NotEqual(Guid.Empty, word.Id);
            Assert.NotEmpty(word.Original);
            Assert.NotEmpty(word.Translation);
            Assert.True(word.IsNew);
            Assert.True(word.IsDue(Fixtures.Teraz));
        });

        Assert.Contains(words, w => w.Original == "scope" && w.Translation == "zakres");
        Assert.Equal(38, words.Select(w => w.Id).Distinct().Count());
    }

    [Fact]
    public void PakietStartowy_ZachowujePolskieZnakiPoPrzejsciuPrzezJson()
    {
        using var dir = new TempDirectory();
        new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).ImportLegacyIfEmpty(PakietStartowy);

        // Ponowny odczyt z dysku - sprawdza cala droge tam i z powrotem.
        var reloaded = new JsonWordStore(dir.JsonFile, Fixtures.Zegar()).GetAll();

        Assert.Contains(reloaded, w => w.Translation.Contains("domyślnie"));
        Assert.Contains(reloaded, w => w.Translation.Contains("tłumić"));
    }
}
