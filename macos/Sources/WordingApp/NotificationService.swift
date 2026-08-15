import Foundation
import UserNotifications
import WordingKit

/// Powiadomienia natywne przez UNUserNotificationCenter.
///
/// W odroznieniu od osascript i terminal-notifier ta droga daje przyciski akcji,
/// czyli ocene powtorki wprost z powiadomienia - bez otwierania menu.
///
/// WYMAGA zapakowanej aplikacji: `UNUserNotificationCenter.current()` przewraca sie
/// w golym pliku wykonywalnym, bo nie ma identyfikatora pakietu. Dlatego aplikacje
/// uruchamia sie przez Wording.app (build-app.sh), a nie przez `swift run`.
@MainActor
final class NotificationService {
    nonisolated static let categoryIdentifier = "wording.word"
    nonisolated static let wordIdKey = "wordId"

    /// Rejestruje kategorie z przyciskami i prosi o zgode.
    func prepare() async {
        let center = UNUserNotificationCenter.current()

        let actions = ReviewGrade.ordered.map {
            UNNotificationAction(identifier: $0.actionIdentifier, title: $0.buttonTitle, options: [])
        }

        center.setNotificationCategories([
            UNNotificationCategory(
                identifier: Self.categoryIdentifier,
                actions: actions,
                intentIdentifiers: [],
                options: []
            )
        ])

        _ = try? await center.requestAuthorization(options: [.alert, .sound])
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
    /// zamiast po cichu nic nie pokazywac.
    func authorizationStatus() async -> UNAuthorizationStatus {
        await UNUserNotificationCenter.current().notificationSettings().authorizationStatus
    }
}
