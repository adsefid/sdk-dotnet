using System.Net;

namespace Adsefid.Sdk.Tests.Infrastructure;

internal static class TestClient
{
    public const string ApiKey = "test-api-key";
    public const string BaseUrl = "https://api.test/";

    public static (AdsefidClient Client, FakeHttpMessageHandler Handler) Build(FakeHttpMessageHandler handler)
    {
        var client = new AdsefidClient(new AdsefidClientOptions
        {
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            HttpClient = new HttpClient(handler),
        });

        return (client, handler);
    }

    public static (AdsefidClient Client, FakeHttpMessageHandler Handler) RespondingWith(
        string body,
        HttpStatusCode status = HttpStatusCode.OK) =>
        Build(FakeHttpMessageHandler.Json(body, status));

    public static (AdsefidClient Client, FakeHttpMessageHandler Handler) RespondingWithFixture(
        string fixture,
        HttpStatusCode status = HttpStatusCode.OK) =>
        RespondingWith(Fixtures.Text(fixture), status);
}
