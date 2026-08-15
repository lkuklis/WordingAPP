import Foundation
import Observation
import UserNotifications
import WordingKit

@MainActor
@Observable
final class AppModel {
    /// Jedna instancja na proces - siega po nia zarowno UI, jak i delegat
    /// powiadomien obslugujacy klikniecia w przyciski oceny.
    static let shared = AppModel()

    /// Dostepne odstepy miedzy slowkami. Poprzednia wersja miala na sztywno
    /// 5 sekund i potrafila walic powiadomieniami przez cala noc.
    static let intervalOptions: [(label: String, seconds: TimeInterval)] = [
        ("5 seconds", 5),
        ("30 seconds", 30),
        ("2 minutes", 120),
        ("10 minutes", 600),
        ("1 hour", 3600),
    ]

    private let notifications = NotificationService()
    private var timer: Timer?

    var manager: WordManager?
    var words: [Word] = []
    var lastShown: Word?
    var statusMessage = ""

    var isPaused = false {
        didSet {
            UserDefaults.standard.set(isPaused, forKey: "isPaused")
            restartTimer()
        }
    }

    var intervalSeconds: TimeInterval = 30 {
        didSet {
            UserDefaults.standard.set(intervalSeconds, forKey: "intervalSeconds")
            restartTimer()
        }
    }

    func start() async {
        isPaused = UserDefaults.standard.bool(forKey: "isPaused")

        let saved = UserDefaults.standard.double(forKey: "intervalSeconds")
        if saved > 0 { intervalSeconds = saved }

        do {
            let store = try WordStore()

            // Pierwsze uruchomienie na czystej maszynie - zasiewamy pakietem
            // startowym. Gdy plik juz istnieje (np. zapisany przez powloke
            // .NET), nic sie nie dzieje.
            try store.seedIfEmpty()

            manager = WordManager(store: store)
            refresh()
        } catch {
            statusMessage = "Nie udalo sie wczytac slowek: \(error.localizedDescription)"
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
            statusMessage = "Nie udalo sie zapisac oceny: \(error.localizedDescription)"
            return
        }

        if lastShown?.id == id { lastShown = nil }
        refresh()
    }

    func gradeLastShown(as grade: ReviewGrade) {
        guard let word = lastShown else { return }
        self.grade(id: word.id, as: grade)
    }

    func addWord(original: String, translation: String) -> Bool {
        guard let manager else { return false }

        do {
            _ = try manager.addWord(original: original, translation: translation)
            refresh()
            return true
        } catch {
            return false
        }
    }

    func remove(id: UUID) {
        _ = try? manager?.removeWord(id: id)
        refresh()
    }

    var dueCount: Int {
        let now = Date()
        return words.filter { $0.review.dueUtc <= now }.count
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
        switch await notifications.authorizationStatus() {
        case .denied:
            statusMessage = "System blokuje powiadomienia — wlacz je w Ustawieniach → Powiadomienia → Wording."
        case .authorized, .provisional, .ephemeral:
            statusMessage = ""
        case .notDetermined:
            statusMessage = "Czekam na zgode na powiadomienia."
        @unknown default:
            statusMessage = ""
        }
    }
}
