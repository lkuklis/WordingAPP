import AppKit
import SwiftUI
import WordingKit

@main
struct WordingApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var delegate
    @State private var model = AppModel.shared

    var body: some Scene {
        MenuBarExtra("Wording", systemImage: "character.book.closed") {
            MenuContent(model: model)
        }
        .menuBarExtraStyle(.menu)

        Window("Wording", id: "words") {
            WordListView(model: model)
        }
        .defaultSize(width: 760, height: 560)
    }
}

struct MenuContent: View {
    @Bindable var model: AppModel
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        if let word = model.lastShown {
            Text("\(word.original) — \(word.translation)")

            Button("I know it") { model.gradeLastShown(as: .good) }
            Button("Hard") { model.gradeLastShown(as: .hard) }
            Button("Don't know") { model.gradeLastShown(as: .again) }
        } else {
            Text("No word shown yet")
        }

        Divider()

        Button(model.isPaused ? "Resume" : "Pause") {
            model.isPaused.toggle()
        }

        Menu("Interval") {
            ForEach(AppModel.intervalOptions, id: \.label) { option in
                Button(option.label) { model.intervalSeconds = option.seconds }
            }
        }

        Divider()

        Button("Show words…") {
            openWindow(id: "words")
            NSApp.activate(ignoringOtherApps: true)
        }

        Button("Quit") { NSApplication.shared.terminate(nil) }
            .keyboardShortcut("q")
    }
}
