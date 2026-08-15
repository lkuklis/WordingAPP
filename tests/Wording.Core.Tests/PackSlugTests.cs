using Wording.Core.Packs;

namespace Wording.Core.Tests;

/// <summary>
/// The identifier of a downloaded pack decides which file gets written, so these are
/// the checks standing between an arbitrary URL and the user's data directory.
/// </summary>
public class PackSlugTests
{
    [Theory]
    [InlineData("travel-basics", "travel-basics")]
    [InlineData("Travel-Basics", "travel-basics")]
    [InlineData("a", "a")]
    [InlineData("es2000", "es2000")]
    public void TryNormalize_AcceptsPlainIdentifiers(string id, string expected)
    {
        Assert.True(PackSlug.TryNormalize(id, out var slug));
        Assert.Equal(expected, slug);
    }

    [Theory]
    [InlineData("../words")]
    [InlineData("../../words")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("sets/../words")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\words")]
    [InlineData("words.json")]
    public void TryNormalize_RefusesAnythingThatCouldChooseItsOwnPath(string id)
    {
        Assert.False(PackSlug.TryNormalize(id, out _));
    }

    [Theory]
    [InlineData("con")]
    [InlineData("CON")]
    [InlineData("com1")]
    [InlineData("nul")]
    [InlineData("lpt9")]
    public void TryNormalize_RefusesNamesWindowsReserves(string id)
    {
        // All letters and digits, so the character rule alone would let them through.
        Assert.False(PackSlug.TryNormalize(id, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("with space")]
    [InlineData("zażółć")]
    [InlineData("emoji-\U0001F600")]
    [InlineData("null\0byte")]
    public void TryNormalize_RefusesEverythingOutsideTheAllowList(string id)
    {
        Assert.False(PackSlug.TryNormalize(id, out _));
    }

    [Fact]
    public void TryNormalize_RefusesAnIdentifierLongerThanTheLimit()
    {
        Assert.True(PackSlug.TryNormalize(new string('a', PackLimits.MaxIdLength), out _));
        Assert.False(PackSlug.TryNormalize(new string('a', PackLimits.MaxIdLength + 1), out _));
    }

    [Fact]
    public void Require_ThrowsTheSharedPackError()
    {
        var error = Assert.Throws<WordPackException>(() => PackSlug.Require("../escape"));

        Assert.Equal(PackProblem.UnsafeId, error.Problem);
    }
}
