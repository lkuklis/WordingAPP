import Testing

@testable import WordingKit

/// The identifier of a downloaded pack decides which file gets written, so these are
/// the checks standing between an arbitrary URL and the user's data directory.
@Suite struct PackSlugTests {
    @Test(arguments: [
        ("travel-basics", "travel-basics"),
        ("Travel-Basics", "travel-basics"),
        ("a", "a"),
        ("es2000", "es2000"),
    ])
    func acceptsPlainIdentifiers(id: String, expected: String) {
        #expect(PackSlug.normalize(id) == expected)
    }

    @Test(arguments: [
        "../words", "../../words", "..", ".", "sets/../words",
        "a/b", "a\\b", "/etc/passwd", "C:\\words", "words.json",
    ])
    func refusesAnythingThatCouldChooseItsOwnPath(id: String) {
        #expect(PackSlug.normalize(id) == nil)
    }

    @Test(arguments: ["con", "CON", "com1", "nul", "lpt9"])
    func refusesNamesWindowsReserves(id: String) {
        // All letters and digits, so the character rule alone would let them through.
        // Checked on macOS too: a pack accepted here has to be storable on Windows.
        #expect(PackSlug.normalize(id) == nil)
    }

    @Test(arguments: ["", " ", "-leading", "trailing-", "with space", "zażółć", "emoji-😀"])
    func refusesEverythingOutsideTheAllowList(id: String) {
        #expect(PackSlug.normalize(id) == nil)
    }

    @Test func refusesAnIdentifierLongerThanTheLimit() {
        #expect(PackSlug.normalize(String(repeating: "a", count: PackLimits.maxIdLength)) != nil)
        #expect(PackSlug.normalize(String(repeating: "a", count: PackLimits.maxIdLength + 1)) == nil)
    }

    @Test func requireThrowsTheSharedPackError() {
        #expect(throws: WordPackError.unsafeId) { try PackSlug.require("../escape") }
    }
}
