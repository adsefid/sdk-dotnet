using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Thrown instead of the base <see cref="AdsefidApiException"/> when the API rejects a request for
/// rate limiting: <see cref="WebServiceResponseCode.MessageLimitReached"/>,
/// <see cref="WebServiceResponseCode.RequestLimitReached"/>, or a bare HTTP 429 with an unparseable
/// body (in which case <see cref="AdsefidApiException.Code"/> is <see cref="WebServiceResponseCode.RequestLimitReached"/>
/// and <see cref="AdsefidApiException.Details"/> is <see langword="null"/>).
/// </summary>
public sealed class AdsefidRateLimitException(WebServiceResponseCode code, string name, int httpStatusCode, ApiErrorDetails? details)
    : AdsefidApiException(code, name, httpStatusCode, details);
