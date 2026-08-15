import Foundation
import Testing

@testable import WordingKit

@Suite struct SpacedRepetitionSchedulerTests {
    static let teraz = Fixtures.teraz

    static func nowe() -> ReviewState { .new(now: teraz) }

    @Test func pierwszaUdanaPowtorkaDajeInterwalJednegoDnia() {
        let stan = SpacedRepetitionScheduler.apply(Self.nowe(), grade: .good, now: Self.teraz)

        #expect(stan.repetitions == 1)
        #expect(stan.intervalDays == 1.0)
        #expect(stan.dueUtc == Self.teraz.addingTimeInterval(86_400))
        #expect(stan.lastReviewedUtc == Self.teraz)
    }

    @Test func drugaUdanaPowtorkaDajeInterwalSzesciuDni() {
        var stan = Self.nowe()
        stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: Self.teraz)
        stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: Self.teraz)

        #expect(stan.repetitions == 2)
        #expect(stan.intervalDays == 6.0)
    }

    @Test func trzeciaUdanaPowtorkaMnozyInterwalPrzezLatwosc() {
        var stan = Self.nowe()
        stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: Self.teraz)
        stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: Self.teraz)
        let przed = stan

        stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: Self.teraz)

        #expect(stan.repetitions == 3)
        #expect(abs(stan.intervalDays - przed.intervalDays * stan.easeFactor) < 1e-9)
    }

    /// Liczby wziete z prawdziwego pliku uzytkownika po klikniecu "Hard"
    /// w powloce Avalonii - Swift musi policzyc dokladnie to samo.
    @Test func hardDajeDokladnieTeSamaLatwoscCoWersjaDotNet() {
        let stan = SpacedRepetitionScheduler.apply(Self.nowe(), grade: .hard, now: Self.teraz)

        #expect(abs(stan.easeFactor - 2.36) < 1e-9)
        #expect(stan.repetitions == 1)
        #expect(stan.lapses == 0)
    }

    /// Analogicznie dla "Don't know" - w pliku uzytkownika wyszlo 1.7.
    @Test func againDajeDokladnieTeSamaLatwoscCoWersjaDotNet() {
        let stan = SpacedRepetitionScheduler.apply(Self.nowe(), grade: .again, now: Self.teraz)

        #expect(abs(stan.easeFactor - 1.7) < 1e-9)
        #expect(stan.repetitions == 0)
        #expect(stan.lapses == 1)
    }

    @Test func goldPodnosiLatwoscODziesiecSetnych() {
        let stan = SpacedRepetitionScheduler.apply(Self.nowe(), grade: .good, now: Self.teraz)

        #expect(abs(stan.easeFactor - (ReviewState.defaultEaseFactor + 0.1)) < 1e-9)
    }

    @Test func againUstawiaTerminNaZaDziesiecMinut() {
        let stan = SpacedRepetitionScheduler.apply(Self.nowe(), grade: .again, now: Self.teraz)

        #expect(stan.dueUtc == Self.teraz.addingTimeInterval(600))
    }

    @Test func latwoscNieSpadaPonizejProgu() {
        var stan = Self.nowe()

        for _ in 0..<50 {
            stan = SpacedRepetitionScheduler.apply(stan, grade: .again, now: Self.teraz)
        }

        #expect(stan.easeFactor == ReviewState.minimumEaseFactor)
    }

    @Test func dobrzeZnaneSlowkoSzybkoOsiagaDlugieInterwaly() {
        var stan = Self.nowe()
        var czas = Self.teraz

        for _ in 0..<6 {
            stan = SpacedRepetitionScheduler.apply(stan, grade: .good, now: czas)
            czas = stan.dueUtc
        }

        #expect(stan.intervalDays > 90)
    }
}
