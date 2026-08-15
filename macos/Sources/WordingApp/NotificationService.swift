import Foundation
import UserNotifications
import WordingKit

/// Powiadomienia natywne przez UNUserNotificationCenter.
///
/// W odroznieniu od osascript i terminal-notifier ta droga daje przyciski akcji,
/// czyli ocene powtorki wprost z powiadomienia - bez otwierania menu.
///
/// WYMAGA zapakowanej aplikacji: UNUserNotificationCenter.current() przewraca sie
/// w golym pliku wykonywalnym, bo nie ma identyfikatora pakietu. Dlatego aplikacje
/// uruchamia sie przez Wording.app, a nie przez `swift run`.
/// Poza klasa i poza izolacja MainActor, bo odczytuje to delegat powiadomien,
/// ktorego metody sa nieizolowane.
enum NotificationAction: String, CaseIterable, Sendable {
    case good = "wording.grade.good"
    case hard = "wording.grade.hard"
    case again = "wording.grade.again"

    var title: String {
        switch self {
        case .good: "I know it"
        case .hard: "Hard"
        case .again: "Don't know"
        }
    }

    var grade: ReviewGrade {
        switch self {
        case .good: .good
        case .hard: .hard
        case .again: .again
        }
    }
}

@MainActor
final class NotificationService {
    nonisolated static let categoryIdentifier = "wording.word"
    nonisolated static let wordIdKey = "wordId"

    private(set) var authorizationDenied = false

    /// Rejestruje kategorie z przyciskami i prosi o zgode.
    func prepare() async {
        let center = UNUserNotificationCenter.current()

        let actions = NotificationAction.allCases.map {
            UNNotificationAction(identifier: $0.rawValue, title: $0.title, options: [])
        }

        center.setNotificationCategories([
            UNNotificationCategory(
                identifier: Self.categoryIdentifier,
                actions: actions,
                intentIdentifiers: [],
                options: []
            )
        ])

        do {
            let granted = try await center.requestAuthorization(options: [.alert, .sound])
            authorizationDenied = !granted
        } catch {
            authorizationDenied = true
        }
    }

    func show(word: Word) {
        let content = UNMutableNotificationContent()
        content.title = word.original
        content.body = word.translation
        content.categoryIdentifier = Self.categoryIdentifier
        content.userInfo = [Self.wordIdKey: word.id.uuidString]

        // Bez wyzwalacza - powiadomienie idzie natychmiast.
        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )

        UNUserNotificationCenter.current().add(request)
    }

    /// Stan zgody, zeby aplikacja mogla powiedziec wprost, ze system ja blokuje,
    /// zamiast po cichu nic nie pokazywac - na tym potknelismy sie z osascript.
    func authorizationStatus() async -> UNAuthorizationStatus {
        await UNUserNotificationCenter.current().notificationSettings().authorizationStatus
    }
}
