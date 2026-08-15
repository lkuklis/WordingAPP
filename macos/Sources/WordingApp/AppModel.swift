import Foundation
import Observation
import WordingKit

@MainActor
@Observable
final class AppModel {
    /// One instance per process - both the UI and the notification delegate that
    /// handles grade button taps reach for it.
    static let shared = AppModel()

    /// Available intervals between words.
    static let intervalOptions: [(label: String, seconds: TimeInterval)] = [
        ("5 seconds", 5),
        ("30 seconds", 30),
        ("2 minutes", 120),
        ("10 minutes", 600),
        ("1 hour", 3600),
    ]

    private enum Keys {
        static let paused = "isPaused"
        static let interval = "intervalSeconds"
    }

    private let notifications = NotificationService()
    private var timer: Timer?

    var manager: WordManager?
    var words: [Word] = []
    var lastShown: Word?
    var statusMessage = ""

    // Initial values are read in the property initialiser, where didSet does not run
    // yet - otherwise startup would write back to UserDefaults what it had just read
    // and would build the timer three times.
    var isPaused = UserDefaults.standard.bool(forKey: Keys.paused) {
        didSet {
            UserDefaults.standard.set(isPaused, forKey: Keys.paused)
            restartTimer()
        }
    }

    var intervalSeconds = UserDefaults.standard.object(forKey: Keys.interval) as? TimeInterval ?? 30 {
        didSet {
            UserDefaults.standard.set(intervalSeconds, forKey: Keys.interval)
            restartTimer()
        }
    }

    func start() async {
        do {
            let store = try WordStore()

            // First run on a clean machine. When the file already exists (written by
            // the .NET app, say) nothing happens.
            try store.seedIfEmpty()

            manager = WordManager(store: store)
            refresh()
        } catch {
            statusMessage = "Could not load words: \(error.localizedDescription)"
            return
        }

        await notifications.prepare()
        await updateAuthorizationMessage()

        restartTimer()
    }

    func refresh() {
        words = (manager?.words ?? []).sorted { $0.review.dueUtc < $1.review.dueUtc }
    }

    func showNextWord() {
        guard let manager, let word = manager.nextWordToShow() else { return }

        lastShown = word
        notifications.show(word: word)
    }

    func grade(id: UUID, as grade: ReviewGrade) {
        guard let manager else { return }

        do {
            _ = try manager.grade(id: id, grade: grade)
        } catch {
            statusMessage = "Could not save the grade: \(error.localizedDescription)"
            return
        }

        if lastShown?.id == id { lastShown = nil }
        refresh()
    }

    func gradeLastShown(as grade: ReviewGrade) {
        guard let word = lastShown else { return }
        self.grade(id: word.id, as: grade)
    }

    @discardableResult
    func addWord(original: String, translation: String) -> Bool {
        guard let manager, (try? manager.addWord(original: original, translation: translation)) != nil
        else { return false }

        refresh()
        return true
    }

    func remove(id: UUID) {
        _ = try? manager?.removeWord(id: id)
        refresh()
    }

    var dueCount: Int {
        let now = Date()
        return words.count { $0.isDue(at: now) }
    }

    private func restartTimer() {
        timer?.invalidate()
        timer = nil

        guard !isPaused else { return }

        timer = Timer.scheduledTimer(withTimeInterval: intervalSeconds, repeats: true) { _ in
            Task { @MainActor in self.showNextWord() }
        }
    }

    private func updateAuthorizationMessage() async {
        statusMessage =
            switch await notifications.authorizationStatus() {
            case .denied:
                "Notifications are blocked — enable them in Settings → Notifications → Wording."
            case .notDetermined: "Waiting for notification permission."
            default: ""
            }
    }
}
