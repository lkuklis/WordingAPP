import SwiftUI
import WordingKit

/// Two ways in: the published catalogue, and any other address.
///
/// The catalogue is fetched when the window opens, so the list is current; the app makes
/// no network request until then. Downloading a pack from an address the user pasted
/// still shows a preview before anything is written, because there is nothing else
/// vouching for it - the catalogue's packs are validated in CI before they are published,
/// which is why a double click there imports straight away.
struct ImportPackView: View {
    @Bindable var model: AppModel
    @Environment(\.dismiss) private var dismiss

    @State private var entries: [PackIndexEntry] = []
    @State private var indexURL: URL?
    @State private var isLoadingCatalogue = false
    @State private var catalogueProblem: String?
    @State private var busyId: String?
    @State private var refreshing: PackIndexEntry?

    @State private var address = ""
    @State private var pack: WordPack?
    @State private var packURL: URL?
    @State private var isDownloading = false
    @State private var problem: String?
    @State private var outcome: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            catalogue

            Divider()

            fromAddress

            if let outcome {
                Label(outcome, systemImage: "checkmark.circle")
                    .foregroundStyle(.green)
                    .font(.callout)
            }

            HStack {
                Spacer()
                Button("Close") { dismiss() }
            }
        }
        .padding(20)
        .frame(width: 560, height: 520)
        .task { await loadCatalogue() }
        .confirmationDialog(
            "You already have \(refreshing?.name ?? "this set")",
            isPresented: .init(get: { refreshing != nil }, set: { if !$0 { refreshing = nil } }),
            titleVisibility: .visible
        ) {
            Button("Add any new words") {
                if let entry = refreshing { install(entry, replaceExisting: true) }
                refreshing = nil
            }
            Button("Cancel", role: .cancel) { refreshing = nil }
        } message: {
            Text("Your review progress on the words you already have is kept.")
        }
    }

    private var catalogue: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text("Packs published with Wording").font(.headline)

                if isLoadingCatalogue {
                    ProgressView().controlSize(.small)
                }

                Spacer()

                Button("Reload") { Task { await loadCatalogue() } }
                    .disabled(isLoadingCatalogue)
            }

            Text("Double-click to download one. Fetched from the Wording repository.")
                .font(.caption)
                .foregroundStyle(.secondary)

            if let catalogueProblem {
                Label(catalogueProblem, systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.orange)
                    .font(.callout)
            }

            List(entries) { entry in
                row(entry)
                    .contentShape(Rectangle())
                    .onTapGesture(count: 2) { open(entry) }
            }
            .frame(minHeight: 200)
        }
    }

    private func row(_ entry: PackIndexEntry) -> some View {
        HStack(alignment: .top) {
            VStack(alignment: .leading, spacing: 2) {
                Text(entry.name)

                if let description = entry.description {
                    Text(description)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Text("\(entry.wordCount) \(PackKind.isConcepts(entry.kind) ? "terms" : "words")")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            if busyId == entry.id {
                ProgressView().controlSize(.small)
            } else if model.haveSet(id: entry.id) {
                Label("Installed", systemImage: "checkmark.circle.fill")
                    .labelStyle(.iconOnly)
                    .foregroundStyle(.green)
                    .help("Already downloaded")
            }
        }
        .padding(.vertical, 2)
    }

    private var fromAddress: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("From another address").font(.headline)

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
        }
    }

    @ViewBuilder
    private func preview(_ pack: WordPack) -> some View {
        let known = model.alreadyHave(pack)
        let fresh = model.newWordCount(in: pack)

        GroupBox {
            VStack(alignment: .leading, spacing: 6) {
                Text(pack.name)

                if let description = pack.description {
                    Text(description).font(.caption).foregroundStyle(.secondary)
                }

                Text("\(pack.words.count) entries").font(.caption).foregroundStyle(.secondary)

                if known {
                    // Never a silent overwrite: an existing set holds review progress.
                    Text("You already have this set. Importing adds the \(fresh) entries it does not have yet.")
                        .font(.callout)
                }

                HStack {
                    Spacer()
                    Button(known ? "Add \(fresh) new" : "Import as a new set") {
                        performImport(pack, from: packURL, replaceExisting: known)
                    }
                    .keyboardShortcut(.defaultAction)
                    .disabled(known && fresh == 0)
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
        }
    }

    // MARK: - Catalogue

    private func loadCatalogue() async {
        isLoadingCatalogue = true
        catalogueProblem = nil

        defer { isLoadingCatalogue = false }

        do {
            let (url, downloaded) = try await model.downloadCatalogue()
            indexURL = url
            entries = downloaded
        } catch {
            catalogueProblem = error.packMessage
        }
    }

    private func open(_ entry: PackIndexEntry) {
        guard busyId == nil else { return }

        if model.haveSet(id: entry.id) {
            refreshing = entry
            return
        }

        install(entry, replaceExisting: false)
    }

    private func install(_ entry: PackIndexEntry, replaceExisting: Bool) {
        guard let indexURL else { return }

        busyId = entry.id
        problem = nil
        outcome = nil

        Task {
            defer { busyId = nil }

            do {
                // Built from the entry's identifier and the catalogue's own address, so
                // the file being downloaded cannot choose where the app looks.
                let url = try PackSource.packURL(index: indexURL, id: entry.id)
                let downloaded = try await model.download(url)

                performImport(downloaded, from: url, replaceExisting: replaceExisting)
            } catch {
                problem = error.packMessage
            }
        }
    }

    // MARK: - From an address

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
                pack = try await model.download(url)
                packURL = url
            } catch {
                problem = error.packMessage
            }
        }
    }

    private func performImport(_ pack: WordPack, from url: URL?, replaceExisting: Bool) {
        do {
            let result = try model.importPack(pack, from: url, replaceExisting: replaceExisting)

            outcome = result.added == 0
                ? "Nothing new to add - you already have every entry in \(result.set.name)."
                : "Added \(result.added) entries to \(result.set.name). Pick it under Learning set to start."
            self.pack = nil
        } catch {
            problem = error.packMessage
        }
    }
}
