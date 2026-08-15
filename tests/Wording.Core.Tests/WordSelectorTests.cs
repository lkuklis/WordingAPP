using Wording.Core;
using Wording.Core.Learning;

namespace Wording.Core.Tests;

public class WordSelectorTests
{
    static readonly DateTimeOffset Now = Fixtures.Now;

    static Word Word(string original, DateTimeOffset? due = null, bool reviewed = true) => new()
    {
        Original = original,
        Translation = original + "-translated",
        CreatedUtc = Now,
        Review = new ReviewState
        {
            DueUtc = due ?? Now,
            LastReviewedUtc = reviewed ? Now.AddDays(-1) : null,
            Repetitions = reviewed ? 1 : 0,
        },
    };

    [Fact]
    public void EmptyList_ReturnsNull()
    {
        Assert.Null(WordSelector.PickNext([], Now, new Random(1234)));
    }

    [Fact]
    public void SingleWord_ReturnsThatWord()
    {
        var word = Word("scope");

        Assert.Same(word, WordSelector.PickNext([word], Now, new Random(1234)));
    }

    [Fact]
    public void AlwaysReturnsAWordFromTheGivenList()
    {
        var words = new[] { Word("a"), Word("b"), Word("c") };
        var random = new Random(1234);

        for (var i = 0; i < 500; i++)
        {
            Assert.Contains(WordSelector.PickNext(words, Now, random), words);
        }
    }

    [Fact]
    public void NewWord_OutweighsAWordThatIsExactlyDue()
    {
        var fresh = Word("new", reviewed: false);
        var due = Word("due", Now);

        Assert.True(WordSelector.Weight(fresh, Now) > WordSelector.Weight(due, Now));
    }

    [Fact]
    public void Weight_GrowsWithLateness()
    {
        var onTime = WordSelector.Weight(Word("on-time", Now), Now);
        var oneDay = WordSelector.Weight(Word("one-day", Now.AddDays(-1)), Now);
        var oneWeek = WordSelector.Weight(Word("one-week", Now.AddDays(-7)), Now);

        Assert.True(onTime < oneDay);
        Assert.True(oneDay < oneWeek);
    }

    [Fact]
    public void Weight_IsCapped()
    {
        // One word forgotten years ago must not dominate the whole rotation.
        var year = WordSelector.Weight(Word("old", Now.AddYears(-1)), Now);
        var decade = WordSelector.Weight(Word("ancient", Now.AddYears(-10)), Now);

        Assert.Equal(year, decade);
    }

    [Fact]
    public void WordNotYetDue_KeepsASmallButNonZeroWeight()
    {
        var weight = WordSelector.Weight(Word("known", Now.AddDays(30)), Now);

        Assert.True(weight > 0, "a well-known word must not drop out of rotation entirely");
        Assert.True(weight < WordSelector.DueWeight);
    }

    [Fact]
    public void OverdueWord_IsDrawnFarMoreOftenThanARecentlyReviewedOne()
    {
        var overdue = Word("forgotten", Now.AddDays(-10));
        var known = Word("known", Now.AddDays(30));
        var words = new[] { overdue, known };
        var random = new Random(1234);

        var hits = 0;
        const int Attempts = 2000;

        for (var i = 0; i < Attempts; i++)
        {
            if (ReferenceEquals(WordSelector.PickNext(words, Now, random), overdue))
            {
                hits++;
            }
        }

        // The weights are roughly 11.0 against 0.032, so the overdue word should take
        // well over 90% of impressions. A loose threshold keeps the test from flaking.
        Assert.True(hits > Attempts * 0.9, $"the overdue word was picked {hits}/{Attempts} times");
    }

    [Fact]
    public void WithEqualWeights_TheDistributionIsRoughlyUniform()
    {
        var words = new[] { Word("a"), Word("b"), Word("c") };
        var random = new Random(1234);
        var counts = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0, ["c"] = 0 };

        for (var i = 0; i < 3000; i++)
        {
            counts[WordSelector.PickNext(words, Now, random)!.Original]++;
        }

        foreach (var (word, count) in counts)
        {
            Assert.True(count is > 800 and < 1200, $"{word} was picked {count} times, expected about 1000");
        }
    }
}
