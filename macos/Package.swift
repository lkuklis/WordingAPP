// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "Wording",
    platforms: [
        // MenuBarExtra needs macOS 13; we take 14 for the newer SwiftUI API.
        .macOS(.v14)
    ],
    targets: [
        // Logic: a port of Wording.Core. It reads and writes exactly the same
        // words.json as the .NET app - the format is the contract between them.
        .target(name: "WordingKit"),

        .executableTarget(
            name: "WordingApp",
            dependencies: ["WordingKit"]
        ),

        .testTarget(
            name: "WordingKitTests",
            dependencies: ["WordingKit"]
        ),
    ]
)
