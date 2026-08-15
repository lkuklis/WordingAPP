import AppKit
import UserNotifications

/// Odbiera reakcje na powiadomienia. Delegat musi byc ustawiony zanim
/// poprosimy o zgode, inaczej klikniecia w przyciski nie trafiaja nigdzie.
final class AppDelegate: NSObject, NSApplicationDelegate, UNUserNotificationCenterDelegate {

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Kolejnosc ma znaczenie: delegat musi byc na miejscu przed prosba o zgode.
        UNUserNotificationCenter.current().delegate = self

        Task { @MainActor in
            await AppModel.shared.start()
        }
    }

    /// Bez tego powiadomienia nie pokazuja sie, gdy aplikacja jest aktywna -
    /// a ona jest aktywna zawsze, bo siedzi w pasku menu.
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
            let id = UUID(uuidString: raw)
        else { return }

        // Klikniecie w samo powiadomienie (bez przycisku) tylko je zamyka.
        guard let action = NotificationAction(rawValue: response.actionIdentifier) else {
            return
        }

        await MainActor.run {
            AppModel.shared.grade(id: id, as: action.grade)
        }
    }
}
