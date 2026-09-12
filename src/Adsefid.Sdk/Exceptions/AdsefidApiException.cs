using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Thrown when the adsefid.com API returns an error response (a non-2xx HTTP status with a parseable
/// error body). <see cref="AdsefidRateLimitException"/> is thrown instead for the two rate-limit
/// codes; check for that subtype first if you handle it separately.
/// </summary>
public class AdsefidApiException : AdsefidException
{
    public AdsefidApiException(WebServiceResponseCode code, string name, int httpStatusCode, ApiErrorDetails? details)
        : base($"adsefid.com API returned error '{name}' ({(int)code}), HTTP status {httpStatusCode}.")
    {
        Code = code;
        Name = name;
        HttpStatusCode = httpStatusCode;
        Details = details;
    }

    /// <summary>The API's numeric error code. May be an unnamed value if the API has added codes since this SDK's <see cref="WebServiceResponseCode"/> enum was last updated.</summary>
    public WebServiceResponseCode Code { get; }

    /// <summary>The API's error name string (e.g. <c>"NOT_ENOUGH_CREDIT"</c>), as returned verbatim by the server.</summary>
    public string Name { get; }

    /// <summary>The HTTP status code of the response.</summary>
    public int HttpStatusCode { get; }

    /// <summary>
    /// Machine-readable field and per-item errors, or <see langword="null"/> when the API omitted details.
    /// </summary>
    public ApiErrorDetails? Details { get; }
}
