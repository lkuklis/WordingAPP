import Foundation

/// Decides where the user's data lives.
///
/// This must point at exactly the same place as `Wording.Core.Storage.WordingPaths`
/// on the .NET side, because both apps work on one file.
public enum WordingPaths {
    public static let appFolderName = "Wording"
    public static let dataFileName = "words.json"

    public static func dataDirectory() -> URL {
        let library = FileManager.default.homeDirectoryForCurrentUser
            .appending(path: "Library", directoryHint: .isDirectory)
            .appending(path: "Application Support", directoryHint: .isDirectory)

        return library.appending(path: appFolderName, directoryHint: .isDirectory)
    }

    public static func dataFile() -> URL {
        dataDirectory().appending(path: dataFileName, directoryHint: .notDirectory)
    }

    /// Imported sets live beside words.json, one file each, never inside it.
    public static let setsFolderName = "sets"

    public static func setsDirectory() -> URL {
        dataDirectory().appending(path: setsFolderName, directoryHint: .isDirectory)
    }

    /// The file an imported set is written to. The identifier has already been through
    /// `PackSlug`, which is what stops a downloaded file from choosing its own path.
    public static func setFile(_ slug: String, in setsDirectory: URL? = nil) -> URL {
        (setsDirectory ?? Self.setsDirectory())
            .appending(path: "\(slug).json", directoryHint: .notDirectory)
    }
}
