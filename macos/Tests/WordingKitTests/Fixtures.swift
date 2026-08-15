import Foundation

@testable import WordingKit

/// A single reference point for every test.
enum Fixtures {
    static let now = Date(timeIntervalSince1970: 1_786_000_000)
}

/// A deterministic generator, so the distribution tests do not flake.
struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64

    init(seed: UInt64) { state = seed &+ 0x9E37_79B9_7F4A_7C15 }

    mutating func next() -> UInt64 {
        state = state &+ 0x9E37_79B9_7F4A_7C15
        var z = state
        z = (z ^ (z >> 30)) &* 0xBF58_476D_1CE4_E5B9
        z = (z ^ (z >> 27)) &* 0x94D0_49BB_1331_11EB
        return z ^ (z >> 31)
    }
}

/// An isolated data directory, removed after the test.
final class TempDirectory {
    let path: URL

    init() throws {
        path = URL.temporaryDirectory.appending(path: "wording-test-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: path, withIntermediateDirectories: true)
    }

    var jsonFile: URL { path.appending(path: WordingPaths.dataFileName) }

    var setsDirectory: URL { path.appending(path: WordingPaths.setsFolderName) }

    func setFile(_ slug: String) -> URL { WordingPaths.setFile(slug, in: setsDirectory) }

    deinit {
        try? FileManager.default.removeItem(at: path)
    }
}
