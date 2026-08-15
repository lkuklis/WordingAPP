import Foundation
import Testing

@testable import WordingKit

@Suite struct SpacedRepetitionSchedulerTests {
    static let now = Fixtures.now

    static func fresh() -> ReviewState { .new(now: now) }

    @Test func firstSuccessfulReviewSchedulesOneDayLater() {
        let state = SpacedRepetitionScheduler.apply(Self.fresh(), grade: .good, now: Self.now)

        #expect(state.repetitions == 1)
        #expect(state.intervalDays == 1.0)
        #expect(state.dueUtc == Self.now.addingTimeInterval(.day))
        #expect(state.lastReviewedUtc == Self.now)
    }

    @Test func secondSuccessfulReviewSchedulesSixDaysLater() {
        var state = Self.fresh()
        state = SpacedRepetitionScheduler.apply(state, grade: .good, now: Self.now)
        state = SpacedRepetitionScheduler.apply(state, grade: .good, now: Self.now)

        #expect(state.repetitions == 2)
        #expect(state.intervalDays == 6.0)
    }

    @Test func thirdSuccessfulReviewMultipliesIntervalByEaseFactor() {
        var state = Self.fresh()
        state = SpacedRepetitionScheduler.apply(state, grade: .good, now: Self.now)
        state = SpacedRepetitionScheduler.apply(state, grade: .good, now: Self.now)
        let beforeThird = state

        state = SpacedRepetitionScheduler.apply(state, grade: .good, now: Self.now)

        #expect(state.repetitions == 3)
        #expect(abs(state.intervalDays - beforeThird.intervalDays * state.easeFactor) < 1e-9)
    }

    /// The numbers come from the user's real data file after tapping "Hard" -
    /// Swift must compute exactly the same value as .NET.
    @Test func hardProducesTheSameEaseFactorAsTheDotNetVersion() {
        let state = SpacedRepetitionScheduler.apply(Self.fresh(), grade: .hard, now: Self.now)

        #expect(abs(state.easeFactor - 2.36) < 1e-9)
        #expect(state.repetitions == 1)
        #expect(state.lapses == 0)
    }

    /// Likewise for "Don't know" - the user's file shows 1.7.
    @Test func againProducesTheSameEaseFactorAsTheDotNetVersion() {
        let state = SpacedRepetitionScheduler.apply(Self.fresh(), grade: .again, now: Self.now)

        #expect(abs(state.easeFactor - 1.7) < 1e-9)
        #expect(state.repetitions == 0)
        #expect(state.lapses == 1)
    }

    @Test func goodRaisesEaseFactorByOneTenth() {
        let state = SpacedRepetitionScheduler.apply(Self.fresh(), grade: .good, now: Self.now)

        #expect(abs(state.easeFactor - (ReviewState.defaultEaseFactor + 0.1)) < 1e-9)
    }

    @Test func againSchedulesTenMinutesLater() {
        let state = SpacedRepetitionScheduler.apply(Self.fresh(), grade: .again, now: Self.now)

        #expect(state.dueUtc == Self.now.addingTimeInterval(600))
    }

    @Test func easeFactorNeverDropsBelowTheFloor() {
        var state = Self.fresh()

        for _ in 0..<50 {
            state = SpacedRepetitionScheduler.apply(state, grade: .again, now: Self.now)
        }

        #expect(state.easeFactor == ReviewState.minimumEaseFactor)
    }

    @Test func wellKnownWordReachesLongIntervalsQuickly() {
        var state = Self.fresh()
        var time = Self.now

        for _ in 0..<6 {
            state = SpacedRepetitionScheduler.apply(state, grade: .good, now: time)
            time = state.dueUtc
        }

        #expect(state.intervalDays > 90)
    }
}
