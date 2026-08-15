import SwiftUI
import WordingKit

struct WordListView: View {
    @Bindable var model: AppModel

    @State private var original = ""
    @State private var translation = ""
    @State private var selection: Word.ID?
    @State private var confirmingDeleteAll = false

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                TextField(model.sideLabels.front, text: $original)
                TextField(model.sideLabels.back, text: $translation)
                Button("Add", action: add)
                    .disabled(original.trimmed.isEmpty || translation.trimmed.isEmpty)
            }

            Table(model.words, selection: $selection) {
                TableColumn(model.sideLabels.front, value: \.original)
                TableColumn(model.sideLabels.back, value: \.translation)
                TableColumn("Reviews") { Text("\($0.review.repetitions)") }
                    .width(70)
                TableColumn("Lapses") { Text("\($0.review.lapses)") }
                    .width(70)
                TableColumn("Next review") { Text(describeDue($0)) }
                    .width(110)
            }
            .overlay {
                if model.words.isEmpty {
                    ContentUnavailableView(
                        "Nothing here yet",
                        systemImage: "character.book.closed",
                        description: Text("Add your first entry above, or import a pack from a URL, and Wording will start showing it in notifications.")
                    )
                }
            }

            HStack {
                Text("Selected:").foregroundStyle(.secondary)

                ForEach(ReviewGrade.ordered, id: \.self) { grade in
                    Button(grade.buttonTitle) { self.grade(grade) }
                }

                Button("Delete", role: .destructive, action: deleteSelected)

                Spacer()
            }
            .disabled(selection == nil)

            HStack {
                // Which set is open matters here: adding and deleting write to it.
                Text("\(model.activeSetName) · \(model.words.count) words · \(model.dueCount) due now")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if !model.statusMessage.isEmpty {
                    Text(model.statusMessage)
                        .font(.caption)
                        .foregroundStyle(.orange)
                }

                Spacer()

                Button("Delete all…", role: .destructive) { confirmingDeleteAll = true }
                    .disabled(model.words.isEmpty)
            }
        }
        .padding(16)
        .task { model.refresh() }
        .confirmationDialog(
            "Delete all \(model.words.count) words?",
            isPresented: $confirmingDeleteAll,
            titleVisibility: .visible
        ) {
            Button("Delete all", role: .destructive) {
                model.removeAll()
                selection = nil
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("Their review progress goes with them. A copy of the file is saved to the backups folder first, so this can still be undone by hand.")
        }
    }

    /// Moves the selection onto the neighbour, so a run of deletions needs one click
    /// each instead of a click and a re-selection.
    private func deleteSelected() {
        guard let selection,
            let index = model.words.firstIndex(where: { $0.id == selection })
        else { return }

        model.remove(id: selection)

        // The row that slid into this index is the following word; past the end of the
        // list, fall back to the new last one.
        self.selection =
            model.words.indices.contains(index)
            ? model.words[index].id
            : model.words.last?.id
    }

    private func add() {
        guard model.addWord(original: original, translation: translation) else { return }

        original = ""
        translation = ""
    }

    private func grade(_ grade: ReviewGrade) {
        guard let selection else { return }
        model.grade(id: selection, as: grade)
    }

    private func describeDue(_ word: Word) -> String {
        let now = Date()

        guard !word.isNew else { return "new" }
        guard !word.isDue(at: now) else { return "due" }

        let remaining = word.review.dueUtc.timeIntervalSince(now)

        if remaining < 3600 { return "in \(max(1, Int(remaining / 60))) min" }
        if remaining < .day { return "in \(Int(remaining / 3600)) h" }

        return "in \(Int(remaining / .day)) d"
    }
}

extension String {
    var trimmed: String { trimmingCharacters(in: .whitespacesAndNewlines) }
}
