namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Base type for every exception this SDK throws. Catch this to handle all SDK failures uniformly,
/// or catch one of its concrete subtypes (<see cref="AdsefidValidationException"/>,
/// <see cref="AdsefidApiException"/>, <see cref="AdsefidRateLimitException"/>,
/// <see cref="AdsefidTransportException"/>, <see cref="AdsefidWebhookVerificationException"/>) to
/// handle a specific failure mode.
/// </summary>
public abstract class AdsefidException : Exception
{
    protected AdsefidException(string message)
        : base(message)
    {
    }

    protected AdsefidException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
