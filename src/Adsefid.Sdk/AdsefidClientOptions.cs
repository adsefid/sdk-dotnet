namespace Adsefid.Sdk;

/// <summary>Configuration for <see cref="AdsefidClient"/>.</summary>
public sealed class AdsefidClientOptions
{
    /// <summary>The adsefid.com API key, sent as the <c>X-API-KEY</c> header on every request.</summary>
    public required string ApiKey { get; init; }

    /// <summary>The API base URL. Defaults to the production endpoint; override only for testing against a different environment.</summary>
    public string BaseUrl { get; init; } = "https://api.adsefid.com";

    /// <summary>
    /// A caller-owned <see cref="System.Net.Http.HttpClient"/> to use instead of the SDK's shared
    /// default. The SDK supplies the absolute request URI and authentication header. Configure
    /// <c>HttpClient.Timeout</c> on a custom client when needed.
    /// </summary>
    public HttpClient? HttpClient { get; init; }
}
