namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Thrown before any network call when a request fails a client-side check the SDK can verify
/// locally (a required field is missing, a string exceeds its documented max length, a collection is
/// empty, etc.). Never thrown by the API itself — see <see cref="AdsefidApiException"/> for that.
/// </summary>
public sealed class AdsefidValidationException(string message) : AdsefidException(message);
