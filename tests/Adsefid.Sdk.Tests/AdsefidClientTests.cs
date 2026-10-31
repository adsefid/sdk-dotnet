using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Messenger.Models;
using Adsefid.Sdk.Sms.Models;
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

    [Theory]
    [InlineData("user-info")]
    [InlineData("sms-cancel")]
    [InlineData("messenger-cancel")]
    [InlineData("sms-status")]
    [InlineData("messenger-status")]
    public async Task ACancelledTokenSurfacesAsOperationCanceled(string operation)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_info.success.json");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> call = operation switch
        {
            "sms-cancel" => () => client.Sms.CancelAsync(new CancelSmsRequest { LocalIds = ["id"] }, cts.Token),
            "messenger-cancel" => () => client.Messenger.CancelAsync(new CancelMessengerRequest { LocalIds = ["id"] }, cts.Token),
            "sms-status" => () => client.Sms.GetStatusAsync(localIds: ["id"], cancellationToken: cts.Token),
            "messenger-status" => () => client.Messenger.GetStatusAsync(localIds: ["id"], cancellationToken: cts.Token),
            _ => () => client.User.GetInfoAsync(cts.Token),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(call);
        Assert.Empty(handler.Requests);
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
    [InlineData(0)]
    [InlineData(2000)]
    [InlineData(2001)]
    public async Task StatusUsesAQueryBodyWithTheCombinedDistinctLimit(int count)
    {
        foreach (var area in new[] { "sms", "messenger" })
        {
            var (client, handler) = TestClient.RespondingWith("""{"status":"success","data":{"receptors":[]}}""");
            var ids = Enumerable.Range(0, count / 2).Select(_ => Guid.NewGuid()).ToArray();
            var locals = Enumerable.Range(0, count - count / 2).Select(i => $"id-{i}").ToArray();
            Func<Task> call = area == "sms"
                ? () => client.Sms.GetStatusAsync(ids.Concat(ids).ToArray(), locals)
                : () => client.Messenger.GetStatusAsync(ids.Concat(ids).ToArray(), locals);
            if (count is 0 or > 2000)
            {
                await Assert.ThrowsAsync<AdsefidValidationException>(call);
                Assert.Empty(handler.Requests);
            }
            else
            {
                await call();
                Assert.Equal("QUERY", handler.Only.Method.Method);
                Assert.Equal($"/v1/{area}/status", handler.Only.Uri.AbsolutePath);
                Assert.Empty(handler.Only.Uri.Query);
                using var body = System.Text.Json.JsonDocument.Parse(handler.Only.Body);
                Assert.Equal(ids, body.RootElement.GetProperty("message_ids").EnumerateArray().Select(id => id.GetGuid()));
                Assert.Equal(locals, body.RootElement.GetProperty("local_ids").EnumerateArray().Select(id => id.GetString()));
            }
        }
    }
}
