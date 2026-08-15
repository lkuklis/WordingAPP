using System.Text;
using Wording.Core.Packs;

namespace Wording.Core.Tests;

public class PackIndexReaderTests
{
    static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    const string Valid = """
        {
          "version": 1,
          "packs": [
            { "id": "spanish-travel", "name": "English → Spanish, travel",
              "description": "Airport words", "kind": "vocabulary", "wordCount": 25 },
            { "id": "it-interview-concepts", "name": "Backend interview concepts",
              "kind": "concepts", "wordCount": 44 }
          ]
        }
        """;

    [Fact]
    public void Read_ReturnsEveryUsableEntry()
    {
        var entries = PackIndexReader.Read(Bytes(Valid));

        Assert.Equal(2, entries.Count);
        Assert.Equal("spanish-travel", entries[0].Id);
        Assert.Equal("English → Spanish, travel", entries[0].Name);
        Assert.Equal("Airport words", entries[0].Description);
        Assert.Equal(25, entries[0].WordCount);
        Assert.Equal(PackKind.Concepts, entries[1].Kind);
    }

    [Theory]
    [InlineData("../../words")]
    [InlineData("/etc/passwd")]
    [InlineData("https://elsewhere.example/evil")]
    [InlineData("con")]
    [InlineData("")]
    public void Read_DropsAnEntryWhoseIdentifierCouldChooseItsOwnAddress(string id)
    {
        // The identifier is the whole of what decides the URL the app will fetch, so a
        // catalogue cannot smuggle one in.
        var json = $$"""
            { "version": 1, "packs": [
              { "id": "{{id}}", "name": "Bad", "wordCount": 1 },
              { "id": "spanish-travel", "name": "Good", "wordCount": 25 }] }
            """;

        var entries = PackIndexReader.Read(Bytes(json));

        Assert.Equal("spanish-travel", Assert.Single(entries).Id);
    }

    [Fact]
    public void Read_KeepsTheGoodRowsWhenOneIsUnusable()
    {
        // A single bad entry must not hide the whole catalogue: it is the only way most
        // people will ever find a pack.
        var json = """
            { "version": 1, "packs": [
              { "id": "no-name", "name": "  ", "wordCount": 3 },
              { "id": "spanish-travel", "name": "Good", "wordCount": 25 }] }
            """;

        Assert.Equal("spanish-travel", Assert.Single(PackIndexReader.Read(Bytes(json))).Id);
    }

    [Fact]
    public void Read_KeepsOnlyTheFirstOfADuplicatedIdentifier()
    {
        var json = """
            { "version": 1, "packs": [
              { "id": "spanish-travel", "name": "First", "wordCount": 1 },
              { "id": "Spanish-Travel", "name": "Second", "wordCount": 2 }] }
            """;

        Assert.Equal("First", Assert.Single(PackIndexReader.Read(Bytes(json))).Name);
    }

    [Fact]
    public void Read_TidiesTextThatWouldBreakTheList()
    {
        var json = """
            { "version": 1, "packs": [
              { "id": "x", "name": "two\nlines ", "description": "\ttabbed", "wordCount": -5 }] }
            """;

        var entry = Assert.Single(PackIndexReader.Read(Bytes(json)));

        Assert.Equal("two lines", entry.Name);
        Assert.Equal("tabbed", entry.Description);
        Assert.Equal(0, entry.WordCount);
    }

    [Fact]
    public void Read_DefaultsAMissingKindToVocabulary()
    {
        var json = """{ "version": 1, "packs": [{ "id": "x", "name": "X", "wordCount": 1 }] }""";

        Assert.Equal(PackKind.Vocabulary, Assert.Single(PackIndexReader.Read(Bytes(json))).Kind);
    }

    [Fact]
    public void Read_StopsAtTheEntryLimit()
    {
        var rows = string.Join(",", Enumerable
            .Range(0, PackLimits.MaxIndexEntries + 50)
            .Select(index => $$"""{ "id": "pack-{{index}}", "name": "P{{index}}", "wordCount": 1 }"""));

        Assert.Equal(
            PackLimits.MaxIndexEntries,
            PackIndexReader.Read(Bytes($$"""{ "version": 1, "packs": [{{rows}}] }""")).Count);
    }

    [Fact]
    public void Read_AnEmptyCatalogueIsNotAnError()
    {
        // Nothing published yet is a state, not a failure.
        Assert.Empty(PackIndexReader.Read(Bytes("""{ "version": 1, "packs": [] }""")));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    public void Read_RefusesWhatIsNotACatalogue(string json)
    {
        Assert.Equal(
            PackProblem.Malformed,
            Assert.Throws<WordPackException>(() => PackIndexReader.Read(Bytes(json))).Problem);
    }

    [Fact]
    public void Read_RefusesAPayloadOverTheLimit()
    {
        var padding = new string('x', PackLimits.MaxPayloadBytes + 1);
        var json = $$"""{ "version": 1, "packs": [{ "id": "x", "name": "{{padding}}", "wordCount": 1 }] }""";

        Assert.Equal(
            PackProblem.TooLarge,
            Assert.Throws<WordPackException>(() => PackIndexReader.Read(Bytes(json))).Problem);
    }

    [Fact]
    public void PackUrl_IsBuiltFromTheIndexAddressAndTheIdentifierAlone()
    {
        var index = new Uri("https://example.com/data/index.json");

        Assert.Equal("https://example.com/data/spanish-travel.json", PackSource.PackUrl(index, "spanish-travel").ToString());

        // A mirror serves the same catalogue without a single address being rewritten.
        Assert.Equal(
            "https://mirror.example.org/wording/spanish-travel.json",
            PackSource.PackUrl(new Uri("https://mirror.example.org/wording/index.json"), "spanish-travel").ToString());
    }

    [Fact]
    public void PackUrl_RefusesAnIdentifierThatIsNotASafeSlug()
    {
        var index = new Uri("https://example.com/data/index.json");

        Assert.Equal(
            PackProblem.UnsafeId,
            Assert.Throws<WordPackException>(() => PackSource.PackUrl(index, "../../secrets")).Problem);
    }
}
