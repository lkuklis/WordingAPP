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

        Window("Import a word pack", id: "import") {
            ImportPackView(model: model)
        }
        .windowResizability(.contentSize)
    }
}

struct MenuContent: View {
    @Bindable var model: AppModel
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        if let word = model.lastShown {
            Text("\(word.original) — \(word.translation)")

            ForEach(ReviewGrade.ordered, id: \.self) { grade in
                Button(grade.buttonTitle) { model.gradeLastShown(as: grade) }
            }
        } else {
            Text("No word shown yet")
        }

        Divider()

        Button(model.isPaused ? "Resume" : "Pause") { model.isPaused.toggle() }

        Menu("Interval") {
            ForEach(AppModel.intervalOptions, id: \.label) { option in
                Button(option.label) { model.intervalSeconds = option.seconds }
            }
        }

        Menu("Learning set — \(model.activeSetName)") {
            Button("My words") { model.switchTo(setId: nil) }

            if !model.sets.isEmpty {
                Divider()

                ForEach(model.sets) { set in
                    Button("\(set.name) (\(set.wordCount))") { model.switchTo(setId: set.id) }
                }
            }

            Divider()

            Button("Import from a URL…") {
                openWindow(id: "import")
                NSApp.activate(ignoringOtherApps: true)
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
