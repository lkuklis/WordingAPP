import SwiftUI
import WordingKit

/// Downloads a word pack and shows what it is before any of it is written.
///
/// The preview step is the point of the screen. A pack comes from an address the user
/// pasted, so the first chance to see whose words these are and how many of them there
/// are should come before they land on disk, not after.
struct ImportPackView: View {
    @Bindable var model: AppModel
    @Environment(\.dismiss) private var dismiss

    @State private var address = ""
    @State private var pack: WordPack?
    @State private var packURL: URL?
    @State private var isDownloading = false
    @State private var problem: String?
    @State private var outcome: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Import a word pack")
                .font(.headline)

            HStack {
                TextField("https://example.com/words.json", text: $address)
                    .textFieldStyle(.roundedBorder)
                    .onSubmit(download)

                Button("Fetch", action: download)
                    .disabled(address.trimmed.isEmpty || isDownloading)
            }

            if isDownloading {
                ProgressView().controlSize(.small)
            }

            if let pack {
                preview(pack)
            }

            if let problem {
                Label(problem, systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.orange)
                    .font(.callout)
            }

            if let outcome {
                Label(outcome, systemImage: "checkmark.circle")
                    .foregroundStyle(.green)
                    .font(.callout)
            }

            Spacer()

            HStack {
                Spacer()
                Button("Close") { dismiss() }
            }
        }
        .padding(20)
        .frame(width: 520, height: 340)
    }

    @ViewBuilder
    private func preview(_ pack: WordPack) -> some View {
        let known = model.alreadyHave(pack)
        let fresh = model.newWordCount(in: pack)

        GroupBox {
            VStack(alignment: .leading, spacing: 6) {
                Text(pack.name).font(.title3)

                if let description = pack.description {
                    Text(description)
                        .font(.callout)
                        .foregroundStyle(.secondary)
                }

                Text("\(pack.words.count) words")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if known {
                    // Never a silent overwrite: an existing set holds review progress.
                    Text("You already have this set. Importing adds the \(fresh) words it does not have yet and leaves your progress on the rest alone.")
                        .font(.callout)
                        .padding(.top, 4)
                }

                HStack {
                    Spacer()
                    Button(known ? "Add \(fresh) new words" : "Import as a new set") {
                        performImport(pack, replaceExisting: known)
                    }
                    .keyboardShortcut(.defaultAction)
                    .disabled(known && fresh == 0)
                }
                .padding(.top, 4)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    private func download() {
        problem = nil
        outcome = nil
        pack = nil

        let url: URL

        do {
            url = try model.address(from: address)
        } catch {
            problem = error.packMessage
            return
        }

        isDownloading = true

        Task {
            defer { isDownloading = false }

            do {
                let downloaded = try await model.download(url)
                pack = downloaded
                packURL = url
            } catch {
                problem = error.packMessage
            }
        }
    }

    private func performImport(_ pack: WordPack, replaceExisting: Bool) {
        guard let packURL else { return }

        do {
            let result = try model.importPack(pack, from: packURL, replaceExisting: replaceExisting)

            outcome = result.added == 0
                ? "Nothing new to add - you already have every word in \(result.set.name)."
                : "Added \(result.added) words to \(result.set.name). Pick it under Learning set to start."
            self.pack = nil
        } catch {
            problem = error.packMessage
        }
    }
}
