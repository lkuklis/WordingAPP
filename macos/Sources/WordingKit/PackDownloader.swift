import Foundation

/// Fetches a pack from a URL and hands back only a validated one.
///
/// The transport is injectable so the rules below can be tested without a network:
/// every check here is one that only ever fires on input nobody sane would send on
/// purpose, which is exactly the kind that goes untested if reaching it needs a real
/// server.
///
/// A port of `Wording.Core.Packs.PackDownloader`.
public struct PackDownloader: Sendable {
    public typealias Fetch = @Sendable (URL) async throws -> (Data, URLResponse)

    private let fetch: Fetch

    public init(fetch: @escaping Fetch = PackDownloader.streamWithCap) {
        self.fetch = fetch
    }

    public func download(from url: URL) async throws -> WordPack {
        try WordPackReader.read(try await fetch(url))
    }

    /// Fetches the published catalogue. Same transport and the same rules as a pack: it
    /// is one more file from the internet, and being the one we publish ourselves is not
    /// a reason to check it less.
    public func downloadIndex(from url: URL) async throws -> [PackIndexEntry] {
        try PackIndexReader.read(try await fetch(url))
    }

    private func fetch(_ url: URL) async throws -> Data {
        try requireHTTPS(url)

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await fetch(url)
        } catch let error as WordPackError {
            throw error
        } catch {
            throw WordPackError.network("could not reach \(url)")
        }

        if let http = response as? HTTPURLResponse {
            guard (200..<300).contains(http.statusCode) else {
                throw WordPackError.network("\(url) answered \(http.statusCode)")
            }
        }

        // A redirect could have moved the request off https on the way here.
        if let finalURL = response.url {
            try requireHTTPS(finalURL)
        }

        guard data.count <= PackLimits.maxPayloadBytes else { throw WordPackError.tooLarge }

        return data
    }

    private func requireHTTPS(_ url: URL) throws {
        guard url.scheme?.lowercased() == "https" else { throw WordPackError.notHttps }
    }

    /// Reads the body as it arrives and gives up as soon as it grows past the limit,
    /// rather than buffering whatever a server decides to send. `URLSession.data(from:)`
    /// would hold the whole response in memory before anyone could object to its size.
    public static let streamWithCap: Fetch = { url in
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = PackLimits.downloadTimeout

        let session = URLSession(configuration: configuration)
        defer { session.finishTasksAndInvalidate() }

        let (bytes, response) = try await session.bytes(from: url)

        var data = Data()
        data.reserveCapacity(64 * 1024)

        for try await byte in bytes {
            data.append(byte)

            if data.count > PackLimits.maxPayloadBytes { throw WordPackError.tooLarge }
        }

        return (data, response)
    }
}
