import Foundation

/// Ustala, gdzie leza dane uzytkownika.
///
/// Musi wskazywac dokladnie to samo miejsce, co `Wording.Core.Storage.WordingPaths`
/// po stronie .NET, bo obie aplikacje pracuja na jednym pliku.
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
