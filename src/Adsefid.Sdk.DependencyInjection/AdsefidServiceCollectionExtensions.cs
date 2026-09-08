using Adsefid.Sdk;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers <see cref="AdsefidClient"/> with Microsoft dependency injection.</summary>
public static class AdsefidServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AdsefidClient"/> as a transient typed HTTP client. Configure transport
    /// behavior on the returned builder. The <paramref name="optionsFactory"/> runs whenever the
    /// container creates a client.
    /// </summary>
    /// <param name="services">Service collection receiving the registration.</param>
    /// <param name="optionsFactory">Creates immutable client options from the current service provider.</param>
    /// <returns>The HTTP client builder for timeout and handler configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/>, <paramref name="optionsFactory"/>, or the factory result is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The factory result supplies <see cref="AdsefidClientOptions.HttpClient"/>. Configure the
    /// returned builder instead so <see cref="IHttpClientFactory"/> manages handler lifetime.
    /// </exception>
    public static IHttpClientBuilder AddAdsefid(
        this IServiceCollection services,
        Func<IServiceProvider, AdsefidClientOptions> optionsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(optionsFactory);

        return services
            .AddHttpClient(nameof(AdsefidClient))
            .AddTypedClient<AdsefidClient>((httpClient, serviceProvider) =>
            {
                var options = optionsFactory(serviceProvider);
                ArgumentNullException.ThrowIfNull(options);

                if (options.HttpClient is not null)
                {
                    throw new InvalidOperationException(
                        $"'{nameof(AdsefidClientOptions.HttpClient)}' must be configured on the returned {nameof(IHttpClientBuilder)}.");
                }

                return new AdsefidClient(new AdsefidClientOptions
                {
                    ApiKey = options.ApiKey,
                    BaseUrl = options.BaseUrl,
                    UserAgent = options.UserAgent,
                    HttpClient = httpClient,
                });
            });
    }
}
