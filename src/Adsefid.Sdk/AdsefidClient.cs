using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
using Adsefid.Sdk.Messenger;
using Adsefid.Sdk.Sms;
using Adsefid.Sdk.User;

namespace Adsefid.Sdk;

/// <summary>
/// Entry point for the adsefid.com SMS Web Service API. Construct one instance per
/// (<see cref="AdsefidClientOptions.BaseUrl"/>, <see cref="AdsefidClientOptions.ApiKey"/>) pair and
/// reuse it — it is safe to share across concurrent calls, and each resource property
/// (<see cref="Sms"/>, <see cref="Messenger"/>, <see cref="User"/>) exposes the operations for that
/// API area.
/// </summary>
public sealed class AdsefidClient
{
    private static readonly HttpClient DefaultHttpClient = new();

    /// <summary>Creates a client from the given <paramref name="options"/>.</summary>
    /// <exception cref="AdsefidValidationException">
    /// <see cref="AdsefidClientOptions.ApiKey"/> is blank, or
    /// <see cref="AdsefidClientOptions.BaseUrl"/> is not an absolute HTTP(S) URL, or
    /// <see cref="AdsefidClientOptions.UserAgent"/> is blank or contains a line break.
    /// </exception>
    public AdsefidClient(AdsefidClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new AdsefidValidationException("'ApiKey' is required.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new AdsefidValidationException("'BaseUrl' must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(options.UserAgent)
            || options.UserAgent.Contains('\r')
            || options.UserAgent.Contains('\n'))
        {
            throw new AdsefidValidationException("'UserAgent' must be non-blank and contain no line breaks.");
        }

        var executor = new RequestExecutor(
            options.HttpClient ?? DefaultHttpClient,
            baseUri,
            options.ApiKey,
            options.UserAgent);

        Sms = new SmsResource(executor);
        Messenger = new MessengerResource(executor);
        User = new UserResource(executor);
    }

    /// <summary>SMS send, status, cancel, and inbound-message operations.</summary>
    public SmsResource Sms { get; }

    /// <summary>Messenger send, file upload, status, and cancel operations.</summary>
    public MessengerResource Messenger { get; }

    /// <summary>Account info, line, profile, and template lookup operations.</summary>
    public UserResource User { get; }
}
