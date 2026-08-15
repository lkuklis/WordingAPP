import SwiftUI
import WordingKit

struct WordListView: View {
    @Bindable var model: AppModel

    @State private var original = ""
    @State private var translation = ""
    @State private var selection: Word.ID?

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                TextField("Word", text: $original)
                TextField("Translation", text: $translation)
                Button("Add", action: add)
                    .disabled(original.trimmed.isEmpty || translation.trimmed.isEmpty)
            }

            Table(model.words, selection: $selection) {
                TableColumn("Word", value: \.original)
                TableColumn("Translation", value: \.translation)
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
                        "No words yet",
                        systemImage: "character.book.closed",
                        description: Text("Add your first word above and Wording will start showing it in notifications.")
                    )
                }
            }

            HStack {
                Text("Selected:").foregroundStyle(.secondary)

                ForEach(ReviewGrade.ordered, id: \.self) { grade in
                    Button(grade.buttonTitle) { self.grade(grade) }
                }

                Button("Delete", role: .destructive) {
                    if let selection { model.remove(id: selection) }
                }

                Spacer()
            }
            .disabled(selection == nil)

            HStack {
                Text("\(model.words.count) words · \(model.dueCount) due now")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if !model.statusMessage.isEmpty {
                    Text(model.statusMessage)
                        .font(.caption)
                        .foregroundStyle(.orange)
                }

                Spacer()
            }
        }
        .padding(16)
        .task { model.refresh() }
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
