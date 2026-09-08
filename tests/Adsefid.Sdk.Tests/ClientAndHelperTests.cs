using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
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
}

public sealed class CsvHelperTests
{
    [Fact]
    public void JoinReturnsNullForNothingToJoin()
    {
        Assert.Null(CsvHelper.Join((IReadOnlyCollection<string>?)null));
        Assert.Null(CsvHelper.Join(Array.Empty<string>()));
        Assert.Null(CsvHelper.Join((IReadOnlyCollection<Guid>?)null));
        Assert.Null(CsvHelper.Join(Array.Empty<Guid>()));
    }

    [Fact]
    public void JoinCommaSeparatesValues()
    {
        Assert.Equal("a", CsvHelper.Join(new[] { "a" }));
        Assert.Equal("a,b,c", CsvHelper.Join(new[] { "a", "b", "c" }));
    }

    [Fact]
    public void BuildIdsQueryOmitsTheQueryStringEntirelyWhenBothAreEmpty()
    {
        Assert.Equal(string.Empty, CsvHelper.BuildIdsQuery(null, null));
        Assert.Equal(string.Empty, CsvHelper.BuildIdsQuery(Array.Empty<Guid>(), Array.Empty<string>()));
    }

    [Fact]
    public void BuildIdsQueryIncludesOnlyThePartsThatHaveValues()
    {
        var id = Guid.NewGuid();

        Assert.Equal($"?message_ids={id}", CsvHelper.BuildIdsQuery([id], null));
        Assert.Equal("?local_ids=l1", CsvHelper.BuildIdsQuery(null, ["l1"]));
        Assert.Equal($"?message_ids={id}&local_ids=l1", CsvHelper.BuildIdsQuery([id], ["l1"]));
    }

    [Fact]
    public void BuildIdsQueryEscapesValues()
    {
        var query = CsvHelper.BuildIdsQuery(null, ["a b", "c&d"]);

        Assert.DoesNotContain(" ", query, StringComparison.Ordinal);
        var parsed = System.Web.HttpUtility.ParseQueryString(query.TrimStart('?'));
        Assert.Equal("a b,c&d", parsed["local_ids"]);
    }
}
