namespace Wording.Core.Learning;

/// <summary>
/// Wybiera nastepne slowko do pokazania.
/// <para>
/// Celowo NIE bramkuje po terminie wymagalnosci, jak klasyczny SRS. Ta aplikacja
/// wyswietla slowko co kilka minut w tle, a nie w sesjach powtorek - przy sztywnych
/// terminach przez wiekszosc czasu nie mialaby czego pokazac. Zamiast tego losuje
/// z wagami: slowka przeterminowane dominuja, dobrze znane pojawiaja sie rzadko,
/// ale nigdy nie znikaja calkiem z rotacji.
/// </para>
/// </summary>
public static class WordSelector
{
    /// <summary>Slowko jeszcze nieocenione ma byc pokazywane czesto, zeby szybko weszlo do obiegu.</summary>
    internal const double NewWordWeight = 10.0;

    /// <summary>Waga slowka dokladnie w terminie. Kazdy dzien opoznienia dodaje 1.</summary>
    internal const double DueWeight = 1.0;

    /// <summary>Gorne ograniczenie opoznienia, zeby jedno zapomniane slowko sprzed roku nie zdominowalo rotacji.</summary>
    internal const double MaxOverdueDays = 30.0;

    /// <summary>Dolna waga slowka swiezo powtorzonego - mala, ale niezerowa, wiec nic nie wypada z rotacji.</summary>
    internal const double MinWeight = 0.02;

    /// <summary>Zwraca slowko do pokazania albo null, jesli lista jest pusta.</summary>
    public static Word? PickNext(IReadOnlyList<Word> words, DateTimeOffset now, Random random)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(random);

        if (words.Count == 0)
        {
            return null;
        }

        var weights = new double[words.Count];
        var total = 0.0;

        for (var i = 0; i < words.Count; i++)
        {
            weights[i] = Weight(words[i], now);
            total += weights[i];
        }

        var roll = random.NextDouble() * total;

        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
            {
                return words[i];
            }
        }

        // Nieosiagalne poza bledami zaokraglen na sumie zmiennoprzecinkowej.
        return words[^1];
    }

    /// <summary>Waga slowka: im pilniejsze, tym wieksza szansa na wylosowanie.</summary>
    internal static double Weight(Word word, DateTimeOffset now)
    {
        if (word.IsNew)
        {
            return NewWordWeight;
        }

        var overdueDays = (now - word.Review.DueUtc).TotalDays;

        if (overdueDays >= 0)
        {
            return DueWeight + Math.Min(overdueDays, MaxOverdueDays);
        }

        // Slowko jeszcze niewymagalne - waga maleje, im dalej do terminu.
        return Math.Max(MinWeight, DueWeight / (1 - overdueDays));
    }
}
