namespace Adsefid.Sdk.Http;

/// <summary>
/// The request bounds the service enforces, checked client-side before a request is built so a
/// caller gets a local <see cref="Exceptions.AdsefidValidationException"/> instead of a round trip.
/// </summary>
internal static class Limits
{
    /// <summary>Maximum SMS message length, in UTF-16 code units.</summary>
    public const int SmsMessageMaxLength = 900;

    /// <summary>Maximum Messenger message length, in UTF-16 code units.</summary>
    public const int MessengerMessageMaxLength = 4000;

    /// <summary>Maximum combined count of distinct message ids and local ids in one lookup.</summary>
    public const int CombinedStatusIdsMax = 2000;

    /// <summary>Minimum accepted value for the inbound-message <c>count</c> parameter.</summary>
    public const int ReceiveCountMin = 1;

    /// <summary>Maximum accepted value for the inbound-message <c>count</c> parameter.</summary>
    public const int ReceiveCountMax = 499;

    /// <summary>Minimum accepted page size when listing templates.</summary>
    public const int TemplatesTakeMin = 1;

    /// <summary>Maximum accepted page size when listing templates.</summary>
    public const int TemplatesTakeMax = 100;
}
