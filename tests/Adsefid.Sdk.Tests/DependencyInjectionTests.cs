using Adsefid.Sdk.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task RegisteredClientUsesFactoryOptionsAndManagedHttpClient()
    {
        var handler = FakeHttpMessageHandler.Json(Fixtures.Text("envelopes/user.get_info.success.json"));
        var services = new ServiceCollection();
        services
            .AddAdsefid(_ => new AdsefidClientOptions
            {
                ApiKey = TestClient.ApiKey,
                BaseUrl = TestClient.BaseUrl,
                UserAgent = "my-service/1.0",
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureHttpClient(httpClient => httpClient.Timeout = TimeSpan.FromSeconds(10));

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AdsefidClient>();
        var second = provider.GetRequiredService<AdsefidClient>();

        Assert.NotSame(first, second);
        await first.User.GetInfoAsync();
        Assert.Equal(new Uri(new Uri(TestClient.BaseUrl), "/v1/user/info"), handler.Only.Uri);
        Assert.Equal(TestClient.ApiKey, handler.Only.Headers["X-API-KEY"]);
        Assert.Equal("my-service/1.0", handler.Only.Headers["User-Agent"]);
    }

    [Fact]
    public void NullOptionsFactoryIsRejected()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddAdsefid(null!));
    }

    [Fact]
    public void NullFactoryResultIsRejectedWhenClientIsResolved()
    {
        var services = new ServiceCollection();
        services.AddAdsefid(_ => null!);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() => provider.GetRequiredService<AdsefidClient>());
    }

    [Fact]
    public void CallerOwnedHttpClientIsRejectedWhenClientIsResolved()
    {
        using var httpClient = new HttpClient();
        var services = new ServiceCollection();
        services.AddAdsefid(_ => new AdsefidClientOptions
        {
            ApiKey = TestClient.ApiKey,
            HttpClient = httpClient,
        });
        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<AdsefidClient>());
    }
}
