using Microsoft.Extensions.Time.Testing;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

/// <summary>
/// Migracja na prawdziwym pakiecie startowym z repozytorium, nie na danych syntetycznych.
/// </summary>
public class StarterPackMigrationTests
{
    static readonly DateTimeOffset Teraz = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    static string PakietStartowy =>
        Path.Combine(AppContext.BaseDirectory, WordingPaths.LegacyDataFileName);

    [Fact]
    public void PakietStartowy_JestDostepnyObokBinarki()
    {
        Assert.True(File.Exists(PakietStartowy), $"nie znaleziono {PakietStartowy}");
    }

    [Fact]
    public void PakietStartowy_MigrujeSieWCalosciIBezPustychWpisow()
    {
        using var katalog = new TempKatalog();

        var magazyn = JsonWordStore.OpenOrMigrate(
            katalog.PlikJson,
            PakietStartowy,
            new FakeTimeProvider(Teraz));

        var slowka = magazyn.GetAll();

        Assert.Equal(38, slowka.Count);
        Assert.All(slowka, slowo =>
        {
            Assert.NotEqual(Guid.Empty, slowo.Id);
            Assert.NotEmpty(slowo.Original);
            Assert.NotEmpty(slowo.Translation);
            Assert.Equal(Teraz, slowo.Review.DueUtc);
            Assert.Null(slowo.Review.LastReviewedUtc);
        });

        Assert.Contains(slowka, s => s.Original == "scope" && s.Translation == "zakres");
        Assert.Equal(38, slowka.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public void PakietStartowy_ZachowujePolskieZnakiPoPrzejsciuPrzezJson()
    {
        using var katalog = new TempKatalog();
        JsonWordStore.OpenOrMigrate(katalog.PlikJson, PakietStartowy, new FakeTimeProvider(Teraz));

        // Ponowny odczyt z dysku - sprawdza cala droge tam i z powrotem.
        var poWczytaniu = new JsonWordStore(katalog.PlikJson, new FakeTimeProvider(Teraz)).GetAll();

        Assert.Contains(poWczytaniu, s => s.Translation.Contains("domyślnie"));
        Assert.Contains(poWczytaniu, s => s.Translation.Contains("tłumić"));
    }
}
