import Foundation
// @preconcurrency because older SDKs (Xcode 16.4 on the CI runner, for instance) ship
// UserNotifications without concurrency annotations: UNNotificationSettings is not
// Sendable there, so returning it across an isolation boundary is a hard error under
// Swift 6. Newer SDKs annotate it and compile either way - this keeps the package
// building on both.
@preconcurrency import UserNotifications
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

    /// Shown at launch while the store is still empty.
    ///
    /// Nothing is seeded any more, so without this a new user sees a menu bar icon and
    /// then silence - which is indistinguishable from permission having been refused.
    /// This one notification proves delivery works and says where to add a word. It
    /// carries no category, so it gets no grading buttons: there is nothing to grade.
    func showWelcome() {
        let content = UNMutableNotificationContent()
        content.title = "Wording is ready"
        content.body = "Add your first word from the menu bar and it will start showing up here."

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
