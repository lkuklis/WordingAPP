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
}
