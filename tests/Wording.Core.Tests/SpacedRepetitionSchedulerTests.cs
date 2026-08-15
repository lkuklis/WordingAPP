using Wording.Core.Learning;

namespace Wording.Core.Tests;

public class SpacedRepetitionSchedulerTests
{
    static readonly DateTimeOffset Teraz = Fixtures.Teraz;

    static ReviewState Nowe() => ReviewState.New(Teraz);

    [Fact]
    public void PierwszaUdanaPowtorka_DajeInterwalJednegoDnia()
    {
        var stan = SpacedRepetitionScheduler.Apply(Nowe(), ReviewGrade.Good, Teraz);

        Assert.Equal(1, stan.Repetitions);
        Assert.Equal(1.0, stan.IntervalDays);
        Assert.Equal(Teraz.AddDays(1), stan.DueUtc);
        Assert.Equal(Teraz, stan.LastReviewedUtc);
    }

    [Fact]
    public void DrugaUdanaPowtorka_DajeInterwalSzesciuDni()
    {
        var stan = Nowe();
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz);
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz.AddDays(1));

        Assert.Equal(2, stan.Repetitions);
        Assert.Equal(6.0, stan.IntervalDays);
    }

    [Fact]
    public void TrzeciaUdanaPowtorka_MnozyInterwalPrzezLatwosc()
    {
        var stan = Nowe();
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz);
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz.AddDays(1));
        var przedTrzecia = stan;

        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz.AddDays(7));

        Assert.Equal(3, stan.Repetitions);
        Assert.Equal(przedTrzecia.IntervalDays * stan.EaseFactor, stan.IntervalDays, precision: 10);
    }

    [Fact]
    public void Good_PodnosiLatwosc()
    {
        var stan = SpacedRepetitionScheduler.Apply(Nowe(), ReviewGrade.Good, Teraz);

        // Wzor SM-2 dla q=5 daje dokladnie +0.1.
        Assert.Equal(ReviewState.DefaultEaseFactor + 0.1, stan.EaseFactor, precision: 10);
    }

    [Fact]
    public void Hard_ObnizaLatwoscAleZaliczaPowtorke()
    {
        var stan = SpacedRepetitionScheduler.Apply(Nowe(), ReviewGrade.Hard, Teraz);

        Assert.True(stan.EaseFactor < ReviewState.DefaultEaseFactor);
        Assert.Equal(1, stan.Repetitions);
        Assert.Equal(0, stan.Lapses);
    }

    [Fact]
    public void Again_ZerujeLicznikPowtorekIZwiekszaLapses()
    {
        var stan = Nowe();
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz);
        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, Teraz.AddDays(1));

        stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Again, Teraz.AddDays(7));

        Assert.Equal(0, stan.Repetitions);
        Assert.Equal(0, stan.IntervalDays);
        Assert.Equal(1, stan.Lapses);
    }

    [Fact]
    public void Again_UstawiaTerminNaZaKilkaMinutAJednakNieNatychmiast()
    {
        // Termin "teraz" sprawilby, ze zapomniane slowko jest stale najbardziej
        // przeterminowane i zablokowaloby cala rotacje.
        var stan = SpacedRepetitionScheduler.Apply(Nowe(), ReviewGrade.Again, Teraz);

        Assert.True(stan.DueUtc > Teraz);
        Assert.True(stan.DueUtc <= Teraz.AddHours(1));
    }

    [Fact]
    public void Latwosc_NieSpadaPonizejProgu()
    {
        var stan = Nowe();

        for (var i = 0; i < 50; i++)
        {
            stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Again, Teraz.AddDays(i));
        }

        Assert.Equal(ReviewState.MinimumEaseFactor, stan.EaseFactor);
    }

    [Fact]
    public void Apply_NieMutujeStanuWejsciowego()
    {
        var przed = Nowe();

        SpacedRepetitionScheduler.Apply(przed, ReviewGrade.Good, Teraz);

        Assert.Equal(0, przed.Repetitions);
        Assert.Equal(0, przed.IntervalDays);
        Assert.Null(przed.LastReviewedUtc);
    }

    [Fact]
    public void DobrzeZnaneSlowko_SzybkoOsiagaDlugieInterwaly()
    {
        var stan = Nowe();
        var czas = Teraz;

        for (var i = 0; i < 6; i++)
        {
            stan = SpacedRepetitionScheduler.Apply(stan, ReviewGrade.Good, czas);
            czas = stan.DueUtc;
        }

        // Po szesciu bezbledych powtorkach slowko ma wracac nie czesciej niz raz na kwartal.
        Assert.True(stan.IntervalDays > 90, $"interwal wyniosl {stan.IntervalDays} dni");
    }
}
