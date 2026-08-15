import Foundation
import Testing

@testable import WordingKit

/// The transport is stubbed on purpose. Every rule below only fires on input nobody
/// sends by accident, which is exactly the kind that stays untested when exercising it
/// needs a real server.
@Suite struct PackDownloaderTests {
    static let address = URL(string: "https://example.com/pack.json")!

    static let valid = """
        { "id": "travel-basics", "name": "Travel basics",
          "words": [{ "original": "airport", "translation": "aeropuerto" }] }
        """

    /// Answers every request with whatever the test decided, without a socket.
    static func stub(
        _ body: String,
        status: Int = 200,
        finalURL: URL? = nil
    ) -> PackDownloader.Fetch {
        { url in
            let response = HTTPURLResponse(
                url: finalURL ?? url,
                statusCode: status,
                httpVersion: nil,
                headerFields: nil
            )!

            return (Data(body.utf8), response)
        }
    }

    @Test func returnsAValidatedPack() async throws {
        let pack = try await PackDownloader(fetch: Self.stub(Self.valid)).download(from: Self.address)

        #expect(pack.id == "travel-basics")
        #expect(pack.words.count == 1)
        #expect(pack.words[0].original == "airport")
    }

    @Test(arguments: [
        "http://example.com/pack.json",
        "ftp://example.com/pack.json",
        "file:///etc/passwd",
    ])
    func acceptsNothingButHttps(address: String) async {
        await #expect(throws: WordPackError.notHttps) {
            try await PackDownloader(fetch: Self.stub(Self.valid))
                .download(from: URL(string: address)!)
        }
    }

    @Test func refusesARedirectThatLeavesHttps() async {
        // The response reports where it ended up; a downgrade there is still a downgrade.
        let fetch = Self.stub(Self.valid, finalURL: URL(string: "http://example.com/pack.json")!)

        await #expect(throws: WordPackError.notHttps) {
            try await PackDownloader(fetch: fetch).download(from: Self.address)
        }
    }

    @Test func reportsAnErrorStatusAsANetworkProblem() async {
        await #expect(throws: WordPackError.network("\(Self.address) answered 404")) {
            try await PackDownloader(fetch: Self.stub("", status: 404)).download(from: Self.address)
        }
    }

    @Test func reportsAnUnreachableHostAsANetworkProblem() async {
        let fetch: PackDownloader.Fetch = { _ in throw URLError(.cannotFindHost) }

        await #expect(throws: WordPackError.network("could not reach \(Self.address)")) {
            try await PackDownloader(fetch: fetch).download(from: Self.address)
        }
    }

    @Test func stopsOnABodyOverTheLimit() async {
        let oversized = String(repeating: "x", count: PackLimits.maxPayloadBytes + 1024)

        await #expect(throws: WordPackError.tooLarge) {
            try await PackDownloader(fetch: Self.stub(oversized)).download(from: Self.address)
        }
    }

    @Test func passesAMalformedBodyToTheReader() async {
        await #expect(throws: WordPackError.self) {
            try await PackDownloader(fetch: Self.stub("this is not json")).download(from: Self.address)
        }
    }
}
