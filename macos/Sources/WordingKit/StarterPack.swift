import Foundation

/// The starter word pack bundled with the app.
///
/// The equivalent of the WordsData.xml import on the .NET side, but in JSON - the
/// macOS port deliberately has no parser for the legacy XML format.
public enum StarterPack {
    struct Entry: Decodable {
        let original: String
        let translation: String
    }

    struct File: Decodable {
        let words: [Entry]
    }

    public static let resourceName = "starter-pack"

    /// Loads the starter pack from the bundle resources.
    public static func load() throws -> [(original: String, translation: String)] {
        guard let url = Bundle.module.url(forResource: resourceName, withExtension: "json") else {
            return []
        }

        let file = try JSONDecoder().decode(File.self, from: Data(contentsOf: url))

        return file.words.map { ($0.original, $0.translation) }
    }
}

extension WordStore {
    /// Seeds the store with the starter pack, but only when it is empty. It never
    /// overwrites data that is already there - including data written by the .NET app,
    /// since both work on one file.
    /// - Returns: how many words were added.
    @discardableResult
    public func seedIfEmpty(now: Date = Date()) throws -> Int {
        guard words.isEmpty else { return 0 }

        let pack = try StarterPack.load()

        guard !pack.isEmpty else { return 0 }

        // One save instead of one per word.
        try append(pack.map {
            Word(original: $0.original, translation: $0.translation, createdUtc: now, review: .new(now: now))
        })

        return pack.count
    }
}
