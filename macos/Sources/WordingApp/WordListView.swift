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
                Button("Add", action: dodaj)
                    .disabled(original.trimmed.isEmpty || translation.trimmed.isEmpty)
            }

            Table(model.words, selection: $selection) {
                TableColumn("Word", value: \.original)
                TableColumn("Translation", value: \.translation)
                TableColumn("Reviews") { Text("\($0.review.repetitions)") }
                    .width(70)
                TableColumn("Lapses") { Text("\($0.review.lapses)") }
                    .width(70)
                TableColumn("Next review") { Text(opisTerminu(for: $0)) }
                    .width(110)
            }

            HStack {
                Text("Selected:").foregroundStyle(.secondary)
                Button("I know it") { ocen(.good) }
                Button("Hard") { ocen(.hard) }
                Button("Don't know") { ocen(.again) }
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

    private func dodaj() {
        guard model.addWord(original: original, translation: translation) else { return }

        original = ""
        translation = ""
    }

    private func ocen(_ grade: ReviewGrade) {
        guard let selection else { return }
        model.grade(id: selection, as: grade)
    }

    private func opisTerminu(for word: Word) -> String {
        guard word.review.lastReviewedUtc != nil else { return "new" }

        let doTerminu = word.review.dueUtc.timeIntervalSinceNow

        if doTerminu <= 0 { return "due" }
        if doTerminu < 3600 { return "in \(max(1, Int(doTerminu / 60))) min" }
        if doTerminu < 86_400 { return "in \(Int(doTerminu / 3600)) h" }

        return "in \(Int(doTerminu / 86_400)) d"
    }
}

extension String {
    var trimmed: String { trimmingCharacters(in: .whitespacesAndNewlines) }
}
