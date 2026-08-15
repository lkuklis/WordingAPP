using System.Text;
using Wording.Core.Packs;

namespace Wording.Core.Tests;

public class WordPackReaderTests
{
    static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    const string Valid = """
        {
          "id": "travel-basics",
          "name": "Travel basics",
          "description": "Everyday phrases",
          "words": [
            { "original": "airport", "translation": "aeropuerto" },
            { "original": "ticket", "translation": "billete" }
          ]
        }
        """;

    [Fact]
    public void Read_AcceptsAWellFormedPack()
    {
        var pack = WordPackReader.Read(Bytes(Valid));

        Assert.Equal("travel-basics", pack.Id);
        Assert.Equal("Travel basics", pack.Name);
        Assert.Equal("Everyday phrases", pack.Description);
        Assert.Equal(2, pack.Words.Count);
        Assert.Equal("aeropuerto", pack.Words[0].Translation);
    }

    [Fact]
    public void Read_KeepsNonAsciiIntact()
    {
        var pack = WordPackReader.Read(Bytes("""
            { "id": "pl", "name": "Polski", "words": [
              { "original": "default", "translation": "domyślnie" }] }
            """));

        Assert.Equal("domyślnie", pack.Words[0].Translation);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"id\": ")]
    [InlineData("[]")]
    public void Read_RefusesWhatIsNotAPack(string json)
    {
        Assert.Equal(PackProblem.Malformed, Problem(json));
    }

    [Fact]
    public void Read_RefusesAPackThatChoosesItsOwnFileName()
    {
        var json = Valid.Replace("travel-basics", "../../words", StringComparison.Ordinal);

        Assert.Equal(PackProblem.UnsafeId, Problem(json));
    }

    [Fact]
    public void Read_RefusesAPackWithNoName()
    {
        Assert.Equal(PackProblem.Malformed, Problem(Valid.Replace("Travel basics", "   ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Read_RefusesAPayloadOverTheLimit()
    {
        // Padding inside the description, so the file stays valid JSON.
        var padding = new string('x', PackLimits.MaxPayloadBytes + 1);
        var json = Valid.Replace("Everyday phrases", padding, StringComparison.Ordinal);

        Assert.Equal(PackProblem.TooLarge, Problem(json));
    }

    [Fact]
    public void Read_RefusesTooManyWords()
    {
        var entries = string.Join(",", Enumerable
            .Range(0, PackLimits.MaxWords + 1)
            .Select(index => $$"""{ "original": "w{{index}}", "translation": "t{{index}}" }"""));

        Assert.Equal(
            PackProblem.TooLarge,
            Problem($$"""{ "id": "big", "name": "Big", "words": [{{entries}}] }"""));
    }

    [Fact]
    public void Read_RefusesAFieldTooLongToBeAWord()
    {
        var essay = new string('a', PackLimits.MaxFieldLength + 1);
        var json = $$"""{ "id": "x", "name": "X", "words": [{ "original": "{{essay}}", "translation": "t" }] }""";

        Assert.Equal(PackProblem.Malformed, Problem(json));
    }

    [Fact]
    public void Read_SkipsBlankEntriesButKeepsTheRest()
    {
        var json = """
            { "id": "x", "name": "X", "words": [
              { "original": "  ", "translation": "empty" },
              { "original": "keep", "translation": "" },
              { "original": "airport", "translation": "aeropuerto" }] }
            """;

        var pack = WordPackReader.Read(Bytes(json));

        Assert.Equal("airport", Assert.Single(pack.Words).Original);
    }

    [Fact]
    public void Read_RefusesAPackWhereNothingUsableIsLeft()
    {
        var json = """{ "id": "x", "name": "X", "words": [{ "original": " ", "translation": " " }] }""";

        Assert.Equal(PackProblem.Empty, Problem(json));
    }

    [Fact]
    public void Read_RefusesAPackWithNoWordsAtAll()
    {
        Assert.Equal(PackProblem.Empty, Problem("""{ "id": "x", "name": "X", "words": [] }"""));
    }

    [Fact]
    public void Read_FoldsControlCharactersThatWouldReachANotification()
    {
        var json = """
            { "id": "x", "name": "X", "words": [
              { "original": "two\nlines", "translation": "\ttabbed " }] }
            """;

        var word = Assert.Single(WordPackReader.Read(Bytes(json)).Words);

        Assert.Equal("two lines", word.Original);
        Assert.Equal("tabbed", word.Translation);
    }

    [Fact]
    public void Read_TruncatesTheNameRatherThanRefusingThePack()
    {
        // Display-only, and the reader cannot expect the user to fix someone else's file.
        var json = Valid.Replace("Travel basics", new string('n', 200), StringComparison.Ordinal);

        Assert.Equal(PackLimits.MaxNameLength, WordPackReader.Read(Bytes(json)).Name.Length);
    }

    static PackProblem Problem(string json) =>
        Assert.Throws<WordPackException>(() => WordPackReader.Read(Bytes(json))).Problem;
}
