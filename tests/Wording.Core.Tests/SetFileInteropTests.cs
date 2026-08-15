using Wording.Core.Packs;
using Wording.Core.Storage;

namespace Wording.Core.Tests;

/// <summary>
/// Pins the JSON both apps have to agree on. The Swift port checks the same shapes from
/// its side in InteropTests.swift; between them the two suites are what stops the ports
/// from drifting, since nothing in either build fails when only one of them changes.
/// </summary>
public class SetFileInteropTests
{
    static readonly Uri Source = new("https://example.com/travel-basics.json");

    [Fact]
    public void ASetFileCarriesTheHeaderKeysTheSwiftPortLooksFor()
    {
        using var dir = new TempDirectory();

        new WordPackImporter(dir.SetsDirectory, Fixtures.Clock()).Import(
            new WordPack
            {
                Id = "travel-basics",
                Name = "Travel basics",
                Words = [new PackEntry { Original = "airport", Translation = "aeropuerto" }],
            },
            Source);

        var json = File.ReadAllText(dir.SetFile("travel-basics"));

        Assert.Contains("\"set\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"travel-basics\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"Travel basics\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sourceUrl\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\"", json, StringComparison.Ordinal);
        Assert.Contains("\"importedUtc\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TheUsersOwnFileNeverGrowsASetHeader()
    {
        using var dir = new TempDirectory();
        var store = new JsonWordStore(dir.JsonFile, Fixtures.Clock());
        store.Add("mine", "moje");

        Assert.DoesNotContain("\"set\"", File.ReadAllText(dir.JsonFile), StringComparison.Ordinal);
    }

    [Fact]
    public void APackWrittenForTheOtherPortIsReadTheSameWay()
    {
        // Byte-for-byte the literal used by InteropTests.readsAPackInTheSharedFormat.
        const string published = """
            {
              "id": "travel-basics",
              "name": "Travel basics",
              "description": "Everyday phrases",
              "words": [{ "original": "airport", "translation": "aeropuerto" }]
            }
            """;

        var pack = WordPackReader.Read(System.Text.Encoding.UTF8.GetBytes(published));

        Assert.Equal("travel-basics", pack.Id);
        Assert.Equal("Everyday phrases", pack.Description);
        Assert.Equal("airport", Assert.Single(pack.Words).Original);
    }

    [Fact]
    public void TheSetsDirectorySitsBesideWordsJson()
    {
        Assert.Equal(
            Path.Combine(WordingPaths.DataDirectory(), "sets", "travel-basics.json"),
            WordingPaths.SetFile("travel-basics"));
    }
}
