import Foundation
import Observation
import WordingKit

/// A row in the sets sidebar. The empty identifier is the user's own words, which are not
/// an imported set and so have no entry in the catalogue.
struct SetChoice: Identifiable, Equatable {
    let id: String
    let name: String
    let count: Int?
    let kind: String?

    var isOwnWords: Bool { id.isEmpty }
    var setId: String? { id.isEmpty ? nil : id }
}

@MainActor
@Observable
final class AppModel {
    /// One instance per process - both the UI and the notification delegate that
    /// handles grade button taps reach for it.
    static let shared = AppModel()

    /// Available intervals between words.
    static let intervalOptions: [(label: String, seconds: TimeInterval)] = [
        ("5 seconds", 5),
        ("30 seconds", 30),
        ("2 minutes", 120),
        ("10 minutes", 600),
        ("1 hour", 3600),
    ]

    private enum Keys {
        static let paused = "isPaused"
        static let interval = "intervalSeconds"
        static let activeSet = "activeSetId"
    }

    private let notifications = NotificationService()
    private let downloader = PackDownloader()
    private let importer = WordPackImporter()
    private var timer: Timer?

    var manager: WordManager?
    var words: [Word] = []
    var lastShown: Word?
    var statusMessage = ""

    /// Imported sets available to switch to, read from disk rather than remembered.
    var sets: [WordSetInfo] = []

    /// nil means the user's own words. Persisted, so the choice survives a restart.
    private(set) var activeSetId: String? = UserDefaults.standard.string(forKey: Keys.activeSet)

    /// What the menu and the list window call the set in use.
    var activeSetName: String {
        guard let activeSetId else { return "My words" }

        return sets.first { $0.id == activeSetId }?.name ?? activeSetId
    }

    /// Everything the user can be learning from, the built-in one first. The sidebar is
    /// built from this rather than from `sets`, so "My words" is a row like any other.
    var choices: [SetChoice] {
        [SetChoice(id: "", name: "My words", count: activeSetId == nil ? words.count : nil, kind: nil)]
            + sets.map { SetChoice(id: $0.id, name: $0.name, count: $0.wordCount, kind: $0.kind) }
    }

    /// Removes an imported set, keeping the file. Falls back to the user's own words when
    /// the set being removed is the one open.
    func remove(setId: String) {
        do {
            if let backup = try WordSetCatalog.remove(setId) {
                statusMessage = "Moved to \(backup.path(percentEncoded: false))"
            }
        } catch {
            statusMessage = "Could not remove the set: \(error.localizedDescription)"
            return
        }

        if activeSetId == setId {
            switchTo(setId: nil)
        } else {
            refresh()
        }
    }

    /// Labels for the two sides. A set of concepts is not a dictionary, and calling its
    /// definitions "translations" makes the list read as though something is missing.
    var sideLabels: (front: String, back: String) {
        guard let activeSetId,
            let set = sets.first(where: { $0.id == activeSetId }),
            PackKind.isConcepts(set.kind)
        else {
            return ("Word", "Translation")
        }

        return ("Term", "Definition")
    }

    // Initial values are read in the property initialiser, where didSet does not run
    // yet - otherwise startup would write back to UserDefaults what it had just read
    // and would build the timer three times.
    var isPaused = UserDefaults.standard.bool(forKey: Keys.paused) {
        didSet {
            UserDefaults.standard.set(isPaused, forKey: Keys.paused)
            restartTimer()
        }
    }

    var intervalSeconds = UserDefaults.standard.object(forKey: Keys.interval) as? TimeInterval ?? 30 {
        didSet {
            UserDefaults.standard.set(intervalSeconds, forKey: Keys.interval)
            restartTimer()
        }
    }

    func start() async {
        guard open(setId: activeSetId) else { return }

        await notifications.prepare()
        await updateAuthorizationMessage()

        // With no words the timer has nothing to show, so this is the only notification
        // a new user would ever get - it is what tells them the app is alive.
        if words.isEmpty { notifications.showWelcome() }

        restartTimer()
    }

    func refresh() {
        words = (manager?.words ?? []).sorted { $0.review.dueUtc < $1.review.dueUtc }
        sets = WordSetCatalog.list()
    }

    /// Switches which set the app is learning from.
    ///
    /// Everything goes through here rather than each screen opening its own store: one
    /// active store per process is the invariant, and a screen left holding the previous
    /// manager would write through a stale in-memory copy.
    func switchTo(setId: String?) {
        guard setId != activeSetId else { return }
        guard open(setId: setId) else { return }

        activeSetId = setId
        UserDefaults.standard.set(setId, forKey: Keys.activeSet)

        restartTimer()
    }

    /// Opens the store for a set and hands the same manager to every screen.
    /// - Returns: false when the file could not be read, having said so in the status.
    @discardableResult
    private func open(setId: String?) -> Bool {
        let fileURL = WordSetCatalog.resolveActiveFile(setId)

        do {
            // The file is created by the first save; a missing one is simply an empty
            // store. Nothing is seeded - the words are the user's own.
            manager = WordManager(store: try WordStore(fileURL: fileURL))
        } catch {
            statusMessage = "Could not load words: \(error.localizedDescription)"
            return false
        }

        // The pending grade belongs to a word from the set being closed. Applying it
        // after the switch would either miss or, worse, hit an unrelated word.
        lastShown = nil
        statusMessage = ""

        refresh()
        return true
    }

    func showNextWord() {
        guard let manager, let word = manager.nextWordToShow() else { return }

        lastShown = word
        notifications.show(word: word)
    }

    func grade(id: UUID, as grade: ReviewGrade) {
        guard let manager else { return }

        do {
            _ = try manager.grade(id: id, grade: grade)
        } catch {
            statusMessage = "Could not save the grade: \(error.localizedDescription)"
            return
        }

        if lastShown?.id == id { lastShown = nil }
        refresh()
    }

    func gradeLastShown(as grade: ReviewGrade) {
        guard let word = lastShown else { return }
        self.grade(id: word.id, as: grade)
    }

    @discardableResult
    func addWord(original: String, translation: String) -> Bool {
        guard let manager, (try? manager.addWord(original: original, translation: translation)) != nil
        else { return false }

        refresh()
        return true
    }

    func remove(id: UUID) {
        _ = try? manager?.removeWord(id: id)

        // The word may be the one the notification is still offering to grade.
        if lastShown?.id == id { lastShown = nil }

        refresh()
    }

    /// Deletes every word. The backup taken first is reported, because the whole point
    /// of it is that the user can find it afterwards.
    func removeAll() {
        guard let manager else { return }

        do {
            if let backup = try manager.removeAllWords() {
                statusMessage = "Backed up to \(backup.path(percentEncoded: false))"
            }
        } catch {
            statusMessage = "Could not delete the words: \(error.localizedDescription)"
            return
        }

        lastShown = nil
        refresh()
    }

    var dueCount: Int {
        let now = Date()
        return words.count { $0.isDue(at: now) }
    }

    // MARK: - Word packs

    /// Reads what the user typed as an address, before anything is fetched.
    func address(from typed: String) throws -> URL {
        let trimmed = typed.trimmingCharacters(in: .whitespacesAndNewlines)

        guard let url = URL(string: trimmed), url.scheme != nil, url.host() != nil else {
            throw WordPackError.malformed("that is not a web address")
        }

        return url
    }

    /// Downloads and validates, without writing anything: the user confirms first.
    func download(_ url: URL) async throws -> WordPack {
        try await downloader.download(from: url)
    }

    /// The published catalogue, and the address it came from - the pack URLs are derived
    /// from that address, so the two travel together.
    func downloadCatalogue() async throws -> (index: URL, entries: [PackIndexEntry]) {
        guard let index = URL(string: PackSource.officialIndexUrl) else {
            throw WordPackError.malformed("the catalogue address is not a URL")
        }

        return (index, try await downloader.downloadIndex(from: index))
    }

    /// Whether a catalogue entry is already on disk, so the list can say so.
    func haveSet(id: String) -> Bool { importer.setExists(id) }

    func alreadyHave(_ pack: WordPack) -> Bool { importer.exists(pack) }

    /// How many of the pack's words a set on disk does not have yet.
    func newWordCount(in pack: WordPack) -> Int {
        guard let fileURL = try? importer.path(for: pack),
            let existing = try? WordStore(fileURL: fileURL)
        else {
            return pack.words.count
        }

        let seen = Set(existing.words.map { key($0.original, $0.translation) })

        return pack.words.count { !seen.contains(key($0.original, $0.translation)) }
    }

    @discardableResult
    func importPack(_ pack: WordPack, from url: URL?, replaceExisting: Bool) throws -> PackImportResult {
        let result = try importer.import(pack, from: url, replaceExisting: replaceExisting)

        refresh()

        // Downloading a set is asking to learn from it. Leaving it to be found in a menu
        // afterwards is the step people were missing.
        switchTo(setId: result.set.id)

        return result
    }

    private func key(_ original: String, _ translation: String) -> String {
        original.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
            + " "
            + translation.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
    }

    private func restartTimer() {
        timer?.invalidate()
        timer = nil

        guard !isPaused else { return }

        timer = Timer.scheduledTimer(withTimeInterval: intervalSeconds, repeats: true) { _ in
            Task { @MainActor in self.showNextWord() }
        }
    }

    private func updateAuthorizationMessage() async {
        statusMessage =
            switch await notifications.authorizationStatus() {
            case .denied:
                "Notifications are blocked — enable them in Settings → Notifications → Wording."
            case .notDetermined: "Waiting for notification permission."
            default: ""
            }
    }
}
