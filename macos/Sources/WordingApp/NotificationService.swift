import Foundation
import UserNotifications
import WordingKit

/// Native notifications through UNUserNotificationCenter.
///
/// Unlike osascript or terminal-notifier, this route gives action buttons - grading a
/// word straight from the notification, without opening a menu.
///
/// It REQUIRES a bundled app: `UNUserNotificationCenter.current()` traps in a bare
/// executable because there is no bundle identifier. Run the app through Wording.app
/// (build-app.sh), not through `swift run`.
@MainActor
final class NotificationService {
    nonisolated static let categoryIdentifier = "wording.word"
    nonisolated static let wordIdKey = "wordId"

    /// Registers the button category and asks for permission.
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

        // No trigger - the notification fires immediately.
        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )

        UNUserNotificationCenter.current().add(request)
    }

    /// Authorization status, so the app can say plainly that the system is blocking it
    /// instead of silently showing nothing.
    func authorizationStatus() async -> UNAuthorizationStatus {
        await UNUserNotificationCenter.current().notificationSettings().authorizationStatus
    }
}
