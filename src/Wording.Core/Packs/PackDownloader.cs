using System.Net.Http;

namespace Wording.Core.Packs;

/// <summary>
/// Fetches a pack from a URL and hands back only a validated one.
/// <para>
/// The <see cref="HttpClient"/> is injectable so the rules below can be tested without
/// a network: every check here is one that only ever fires on input nobody sane would
/// send on purpose, which is exactly the kind that goes untested if reaching it needs
/// a real server.
/// </para>
/// </summary>
public sealed class PackDownloader
{
    readonly HttpClient _client;

    public PackDownloader(HttpClient? client = null)
    {
        _client = client ?? new HttpClient { Timeout = PackLimits.DownloadTimeout };
    }

    public async Task<WordPack> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        RequireHttps(url);

        HttpResponseMessage response;

        try
        {
            response = await _client
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException
                                      && !cancellationToken.IsCancellationRequested)
        {
            throw new WordPackException(PackProblem.Network, $"could not reach {url}", error);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new WordPackException(
                    PackProblem.Network,
                    $"{url} answered {(int)response.StatusCode}");
            }

            // A redirect could have moved the request off https on the way here.
            if (response.RequestMessage?.RequestUri is { } finalUrl)
            {
                RequireHttps(finalUrl);
            }

            // Trusted only as an early exit - a server can lie about it or omit it, so
            // the read below counts bytes regardless.
            if (response.Content.Headers.ContentLength > PackLimits.MaxPayloadBytes)
            {
                throw new WordPackException(
                    PackProblem.TooLarge,
                    $"{url} declares {response.Content.Headers.ContentLength} bytes");
            }

            var payload = await ReadCappedAsync(response, url, cancellationToken).ConfigureAwait(false);

            return WordPackReader.Read(payload);
        }
    }

    static void RequireHttps(Uri url)
    {
        if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new WordPackException(
                PackProblem.NotHttps,
                $"{url.Scheme} is not accepted, a pack must be served over https");
        }
    }

    /// <summary>
    /// Reads the body, giving up as soon as it grows past the limit rather than after
    /// buffering the whole of it.
    /// </summary>
    static async Task<byte[]> ReadCappedAsync(
        HttpResponseMessage response,
        Uri url,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var collected = new MemoryStream();

        try
        {
            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            while (true)
            {
                var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                if (collected.Length + read > PackLimits.MaxPayloadBytes)
                {
                    throw new WordPackException(
                        PackProblem.TooLarge,
                        $"{url} sent more than {PackLimits.MaxPayloadBytes} bytes");
                }

                collected.Write(buffer, 0, read);
            }
        }
        catch (Exception error) when (error is HttpRequestException or IOException or TaskCanceledException
                                      && !cancellationToken.IsCancellationRequested)
        {
            throw new WordPackException(PackProblem.Network, $"the download from {url} broke off", error);
        }

        return collected.ToArray();
    }
}
