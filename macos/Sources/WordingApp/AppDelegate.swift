import AppKit
import UserNotifications
import WordingKit

/// Receives notification responses. The delegate must be set before we ask for
/// permission, otherwise button taps go nowhere.
final class AppDelegate: NSObject, NSApplicationDelegate, UNUserNotificationCenterDelegate {

    func applicationDidFinishLaunching(_ notification: Notification) {
        UNUserNotificationCenter.current().delegate = self

        Task { @MainActor in
            await AppModel.shared.start()
        }
    }

    /// Without this, notifications do not appear while the app is active - and it is
    /// always active, because it lives in the menu bar.
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification
    ) async -> UNNotificationPresentationOptions {
        [.banner, .list]
    }

    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse
    ) async {
        let userInfo = response.notification.request.content.userInfo

        guard
            let raw = userInfo[NotificationService.wordIdKey] as? String,
            let id = UUID(uuidString: raw),
            // Tapping the notification body (no button) just dismisses it.
            let grade = ReviewGrade(actionIdentifier: response.actionIdentifier)
        else { return }

        await MainActor.run {
            AppModel.shared.grade(id: id, as: grade)
        }
    }
}
