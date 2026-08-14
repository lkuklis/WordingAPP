namespace Wording.Core.Learning;

/// <summary>
/// Wybiera nastepne slowko do pokazania.
/// <para>
/// Celowo NIE bramkuje po terminie wymagalnosci, jak klasyczny SRS. Ta aplikacja
/// wyswietla slowko co kilka sekund w tle, a nie w sesjach powtorek - przy sztywnych
/// terminach przez wiekszosc czasu nie mialaby czego pokazac. Zamiast tego losuje
/// z wagami: slowka przeterminowane dominuja, dobrze znane pojawiaja sie rzadko,
/// ale nigdy nie znikaja calkiem z rotacji.
/// </para>
/// </summary>
public sealed class WordSelector
{
    /// <summary>Slowko jeszcze nigdy nieocenione ma byc pokazywane czesto, zeby szybko weszlo do obiegu.</summary>
    internal const double NewWordWeight = 10.0;

    /// <summary>Waga slowka dokladnie w terminie. Kazdy dzien opoznienia dodaje 1.</summary>
    internal const double DueWeight = 1.0;

    /// <summary>Gorne ograniczenie opoznienia, zeby jedno zapomniane slowko sprzed roku nie zdominowalo rotacji.</summary>
    internal const double MaxOverdueDays = 30.0;

    /// <summary>Dolna waga slowka swiezo powtorzonego - male, ale niezerowe, wiec nic nie wypada z rotacji.</summary>
    internal const double MinWeight = 0.02;

    readonly TimeProvider _clock;
    readonly Random _random;

    public WordSelector(TimeProvider? clock = null, Random? random = null)
    {
        _clock = clock ?? TimeProvider.System;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Zwraca slowko do pokazania albo null, jesli lista jest pusta.
    /// </summary>
    public Word? PickNext(IReadOnlyList<Word> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        if (words.Count == 0)
        {
            return null;
        }

        var now = _clock.GetUtcNow();
        var wagi = new double[words.Count];
        var suma = 0.0;

        for (var i = 0; i < words.Count; i++)
        {
            wagi[i] = Weight(words[i], now);
            suma += wagi[i];
        }

        var los = _random.NextDouble() * suma;

        for (var i = 0; i < wagi.Length; i++)
        {
            los -= wagi[i];
            if (los <= 0)
            {
                return words[i];
            }
        }

        // Nieosiagalne poza bledami zaokraglen na sumie zmiennoprzecinkowej.
        return words[^1];
    }

    /// <summary>
    /// Waga slowka: im pilniejsze, tym wieksza szansa na wylosowanie.
    /// </summary>
    internal static double Weight(Word word, DateTimeOffset now)
    {
        if (word.Review.LastReviewedUtc is null)
        {
            return NewWordWeight;
        }

        var opoznienieDni = (now - word.Review.DueUtc).TotalDays;

        if (opoznienieDni >= 0)
        {
            return DueWeight + Math.Min(opoznienieDni, MaxOverdueDays);
        }

        // Slowko jeszcze niewymagalne - waga maleje, im dalej do terminu.
        return Math.Max(MinWeight, DueWeight / (1 - opoznienieDni));
    }
}
