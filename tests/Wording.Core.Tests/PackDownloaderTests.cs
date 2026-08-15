using System.Net;
using System.Text;
using Wording.Core.Packs;

namespace Wording.Core.Tests;

/// <summary>
/// The transport is stubbed on purpose. Every rule below only fires on input nobody
/// sends by accident, which is exactly the kind that stays untested when exercising it
/// needs a real server.
/// </summary>
public class PackDownloaderTests
{
    const string Valid = """
        { "id": "travel-basics", "name": "Travel basics",
          "words": [{ "original": "airport", "translation": "aeropuerto" }] }
        """;

    static readonly Uri Address = new("https://example.com/pack.json");

    [Fact]
    public async Task DownloadAsync_ReturnsAValidatedPack()
    {
        var pack = await Downloader(Respond(Valid)).DownloadAsync(Address);

        Assert.Equal("travel-basics", pack.Id);
        Assert.Equal("airport", Assert.Single(pack.Words).Original);
    }

    [Theory]
    [InlineData("http://example.com/pack.json")]
    [InlineData("ftp://example.com/pack.json")]
    [InlineData("file:///etc/passwd")]
    public async Task DownloadAsync_AcceptsNothingButHttps(string address)
    {
        var error = await Assert.ThrowsAsync<WordPackException>(
            () => Downloader(Respond(Valid)).DownloadAsync(new Uri(address)));

        Assert.Equal(PackProblem.NotHttps, error.Problem);
    }

    [Fact]
    public async Task DownloadAsync_RefusesARedirectThatLeavesHttps()
    {
        // HttpClient reports where it ended up; a downgrade there is still a downgrade.
        var response = Respond(Valid);
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com/pack.json");

        Assert.Equal(PackProblem.NotHttps, await Problem(response));
    }

    [Fact]
    public async Task DownloadAsync_ReportsAnErrorStatusAsANetworkProblem()
    {
        Assert.Equal(PackProblem.Network, await Problem(new HttpResponseMessage(HttpStatusCode.NotFound)));
    }

    [Fact]
    public async Task DownloadAsync_ReportsAnUnreachableHostAsANetworkProblem()
    {
        var downloader = new PackDownloader(new HttpClient(
            new StubHandler(_ => throw new HttpRequestException("no route to host"))));

        var error = await Assert.ThrowsAsync<WordPackException>(() => downloader.DownloadAsync(Address));

        Assert.Equal(PackProblem.Network, error.Problem);
    }

    [Fact]
    public async Task DownloadAsync_StopsOnADeclaredLengthOverTheLimit()
    {
        var response = Respond(Valid);
        response.Content.Headers.ContentLength = PackLimits.MaxPayloadBytes + 1;

        Assert.Equal(PackProblem.TooLarge, await Problem(response));
    }

    [Fact]
    public async Task DownloadAsync_StopsOnABodyOverTheLimitEvenWhenTheLengthIsAbsent()
    {
        // A server can omit Content-Length or lie about it, so the read counts bytes.
        var oversized = new string('x', PackLimits.MaxPayloadBytes + 1024);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(oversized))),
        };
        response.Content.Headers.ContentLength = null;

        Assert.Equal(PackProblem.TooLarge, await Problem(response));
    }

    [Fact]
    public async Task DownloadAsync_PassesAMalformedBodyToTheReader()
    {
        Assert.Equal(PackProblem.Malformed, await Problem(Respond("this is not json")));
    }

    static async Task<PackProblem> Problem(HttpResponseMessage response)
    {
        var error = await Assert.ThrowsAsync<WordPackException>(
            () => Downloader(response).DownloadAsync(Address));

        return error.Problem;
    }

    static PackDownloader Downloader(HttpResponseMessage response) =>
        new(new HttpClient(new StubHandler(_ => response)));

    static HttpResponseMessage Respond(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

/// <summary>Answers every request with whatever the test decided, without a socket.</summary>
sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = respond(request);
        response.RequestMessage ??= request;

        return Task.FromResult(response);
    }
}
