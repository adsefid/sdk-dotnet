using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class AdsefidClientTests
{
    [Theory]
    [InlineData(null, "'ApiKey'")]
    [InlineData("", "'ApiKey'")]
    [InlineData("   ", "'ApiKey'")]
    public void ABlankApiKeyIsRejected(string? apiKey, string expectedField)
    {
        var exception = Assert.Throws<AdsefidValidationException>(
            () => new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey! }));

        Assert.Contains(expectedField, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://api.test")]
    [InlineData("/relative")]
    public void ABaseUrlThatIsNotAbsoluteHttpIsRejected(string baseUrl)
    {
        Assert.Throws<AdsefidValidationException>(
            () => new AdsefidClient(new AdsefidClientOptions { ApiKey = "k", BaseUrl = baseUrl }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\nagent")]
    [InlineData("bad\ragent")]
    public void AnUnusableUserAgentIsRejected(string userAgent)
    {
        Assert.Throws<AdsefidValidationException>(
            () => new AdsefidClient(new AdsefidClientOptions { ApiKey = "k", UserAgent = userAgent }));
    }

    [Fact]
    public void AllThreeResourcesAreWiredUp()
    {
        var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = "k" });

        Assert.NotNull(client.Sms);
        Assert.NotNull(client.Messenger);
        Assert.NotNull(client.User);
    }

    [Fact]
    public async Task EveryRequestCarriesTheApiKeyAndUserAgent()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_info.success.json");

        await client.User.GetInfoAsync();

        var request = handler.Only;
        Assert.Equal(TestClient.ApiKey, request.Headers["X-API-KEY"]);
        // Never assert an exact version: it is derived from the assembly.
        Assert.StartsWith("adsefid-dotnet/", request.Headers["User-Agent"], StringComparison.Ordinal);
        Assert.Contains("application/json", request.Headers["Accept"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACustomUserAgentReachesTheWire()
    {
        var handler = FakeHttpMessageHandler.Json(Fixtures.Text("envelopes/user.get_info.success.json"));
        var client = new AdsefidClient(new AdsefidClientOptions
        {
            ApiKey = "k",
            BaseUrl = TestClient.BaseUrl,
            UserAgent = "my-app/2.1",
            HttpClient = new HttpClient(handler),
        });

        await client.User.GetInfoAsync();

        Assert.Equal("my-app/2.1", handler.Only.Headers["User-Agent"]);
    }

    [Fact]
    public async Task ACancelledTokenSurfacesAsOperationCanceled()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/user.get_info.success.json");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.User.GetInfoAsync(cts.Token));
    }

    [Theory]
    [InlineData("https://api.test/webservice/api")]
    [InlineData("https://api.test/webservice/api/")]
    public async Task ABaseUrlPathIsKeptInFrontOfTheRoute(string baseUrl)
    {
        var handler = FakeHttpMessageHandler.Json("""{"status":"success","data":[]}""");
        var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = "k", BaseUrl = baseUrl, HttpClient = new HttpClient(handler) });

        await client.User.GetLinesAsync();

        Assert.Equal("/webservice/api/v1/user/lines", handler.Only.Uri.AbsolutePath);
    }

    [Theory]
    [InlineData(210, true)]
    [InlineData(230, false)]
    [InlineData(2000, false)]
    public async Task AStatusLookupThatCannotFitInAUrlIsRefusedLocally(int count, bool accepted)
    {
        var (client, handler) = TestClient.RespondingWith("""{"status":"success","data":{"receptors":[]}}""");
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

        if (accepted)
        {
            await client.Sms.GetStatusAsync(ids);
            Assert.DoesNotContain("%2C", handler.Only.Uri.Query, StringComparison.Ordinal);
            return;
        }

        var exception = await Assert.ThrowsAsync<AdsefidValidationException>(() => client.Sms.GetStatusAsync(ids));
        Assert.Contains("characters long", exception.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }
}
