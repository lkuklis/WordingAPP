import Foundation

/// Wybiera nastepne slowko do pokazania.
///
/// Port `Wording.Core.Learning.WordSelector`. Celowo NIE bramkuje po terminie
/// wymagalnosci, jak klasyczny SRS: aplikacja pokazuje slowko co kilka sekund
/// w tle, a nie w sesjach powtorek, wiec przy sztywnych terminach przez
/// wiekszosc czasu nie mialaby czego wyswietlic. Zamiast tego losuje z wagami.
public struct WordSelector: Sendable {
    /// Slowko jeszcze nigdy nieocenione ma byc pokazywane czesto.
    public static let newWordWeight = 10.0

    /// Waga slowka dokladnie w terminie. Kazdy dzien opoznienia dodaje 1.
    public static let dueWeight = 1.0

    /// Gorne ograniczenie opoznienia, zeby jedno zapomniane slowko sprzed roku
    /// nie zdominowalo rotacji.
    public static let maxOverdueDays = 30.0

    /// Dolna waga - mala, ale niezerowa, wiec nic nie wypada z rotacji.
    public static let minWeight = 0.02

    public init() {}

    /// Zwraca slowko do pokazania albo nil, jesli lista jest pusta.
    public func pickNext(
        from words: [Word],
        now: Date,
        using generator: inout some RandomNumberGenerator
    ) -> Word? {
        guard !words.isEmpty else { return nil }

        let weights = words.map { Self.weight(for: $0, now: now) }
        let total = weights.reduce(0, +)

        var roll = Double.random(in: 0..<total, using: &generator)

        for (index, weight) in weights.enumerated() {
            roll -= weight
            if roll <= 0 {
                return words[index]
            }
        }

        // Nieosiagalne poza bledami zaokraglen na sumie zmiennoprzecinkowej.
        return words.last
    }

    public func pickNext(from words: [Word], now: Date) -> Word? {
        var generator = SystemRandomNumberGenerator()
        return pickNext(from: words, now: now, using: &generator)
    }

    /// Waga slowka: im pilniejsze, tym wieksza szansa na wylosowanie.
    public static func weight(for word: Word, now: Date) -> Double {
        guard word.review.lastReviewedUtc != nil else {
            return newWordWeight
        }

        let overdueDays = now.timeIntervalSince(word.review.dueUtc)
            / SpacedRepetitionScheduler.secondsPerDay

        if overdueDays >= 0 {
            return dueWeight + min(overdueDays, maxOverdueDays)
        }

        // Slowko jeszcze niewymagalne - waga maleje, im dalej do terminu.
        return max(minWeight, dueWeight / (1 - overdueDays))
    }
}
