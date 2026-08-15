using Wording.Core.Learning;

namespace Wording.Core.Tests;

public class SpacedRepetitionSchedulerTests
{
    static readonly DateTimeOffset Now = Fixtures.Now;

    static ReviewState Fresh() => ReviewState.New(Now);

    [Fact]
    public void FirstSuccessfulReview_SchedulesOneDayLater()
    {
        var state = SpacedRepetitionScheduler.Apply(Fresh(), ReviewGrade.Good, Now);

        Assert.Equal(1, state.Repetitions);
        Assert.Equal(1.0, state.IntervalDays);
        Assert.Equal(Now.AddDays(1), state.DueUtc);
        Assert.Equal(Now, state.LastReviewedUtc);
    }

    [Fact]
    public void SecondSuccessfulReview_SchedulesSixDaysLater()
    {
        var state = Fresh();
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now);
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now.AddDays(1));

        Assert.Equal(2, state.Repetitions);
        Assert.Equal(6.0, state.IntervalDays);
    }

    [Fact]
    public void ThirdSuccessfulReview_MultipliesIntervalByEaseFactor()
    {
        var state = Fresh();
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now);
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now.AddDays(1));
        var beforeThird = state;

        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now.AddDays(7));

        Assert.Equal(3, state.Repetitions);
        Assert.Equal(beforeThird.IntervalDays * state.EaseFactor, state.IntervalDays, precision: 10);
    }

    [Fact]
    public void Good_RaisesEaseFactor()
    {
        var state = SpacedRepetitionScheduler.Apply(Fresh(), ReviewGrade.Good, Now);

        // The SM-2 formula for q=5 gives exactly +0.1.
        Assert.Equal(ReviewState.DefaultEaseFactor + 0.1, state.EaseFactor, precision: 10);
    }

    [Fact]
    public void Hard_LowersEaseFactorButStillCountsAsSuccess()
    {
        var state = SpacedRepetitionScheduler.Apply(Fresh(), ReviewGrade.Hard, Now);

        Assert.True(state.EaseFactor < ReviewState.DefaultEaseFactor);
        Assert.Equal(1, state.Repetitions);
        Assert.Equal(0, state.Lapses);
    }

    [Fact]
    public void Again_ResetsRepetitionsAndCountsALapse()
    {
        var state = Fresh();
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now);
        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, Now.AddDays(1));

        state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Again, Now.AddDays(7));

        Assert.Equal(0, state.Repetitions);
        Assert.Equal(0, state.IntervalDays);
        Assert.Equal(1, state.Lapses);
    }

    [Fact]
    public void Again_SchedulesShortlyAfterButNotImmediately()
    {
        // A due date of "now" would make a forgotten word permanently the most
        // overdue one and would block the whole rotation.
        var state = SpacedRepetitionScheduler.Apply(Fresh(), ReviewGrade.Again, Now);

        Assert.True(state.DueUtc > Now);
        Assert.True(state.DueUtc <= Now.AddHours(1));
    }

    [Fact]
    public void EaseFactor_NeverDropsBelowTheFloor()
    {
        var state = Fresh();

        for (var i = 0; i < 50; i++)
        {
            state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Again, Now.AddDays(i));
        }

        Assert.Equal(ReviewState.MinimumEaseFactor, state.EaseFactor);
    }

    [Fact]
    public void Apply_DoesNotMutateTheInputState()
    {
        var before = Fresh();

        SpacedRepetitionScheduler.Apply(before, ReviewGrade.Good, Now);

        Assert.Equal(0, before.Repetitions);
        Assert.Equal(0, before.IntervalDays);
        Assert.Null(before.LastReviewedUtc);
    }

    [Fact]
    public void WellKnownWord_ReachesLongIntervalsQuickly()
    {
        var state = Fresh();
        var time = Now;

        for (var i = 0; i < 6; i++)
        {
            state = SpacedRepetitionScheduler.Apply(state, ReviewGrade.Good, time);
            time = state.DueUtc;
        }

        // After six flawless reviews a word should come back no more than once a quarter.
        Assert.True(state.IntervalDays > 90, $"interval was {state.IntervalDays} days");
    }
}
