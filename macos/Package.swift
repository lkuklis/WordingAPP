// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "Wording",
    platforms: [
        // MenuBarExtra wymaga macOS 13; bierzemy 14 dla nowszego API SwiftUI.
        .macOS(.v14)
    ],
    targets: [
        // Logika: port Wording.Core. Czyta i zapisuje dokladnie ten sam
        // words.json, co powloki .NET - format jest kontraktem miedzy nimi.
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
