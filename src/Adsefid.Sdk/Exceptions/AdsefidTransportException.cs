namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Thrown for a failure that occurred outside of a well-formed API error response: a network/DNS
/// failure, a request timeout, or a successful HTTP response whose body could not be parsed into the
/// expected shape. The original exception, if any, is available via <see cref="Exception.InnerException"/>.
/// </summary>
public sealed class AdsefidTransportException(string message, Exception? innerException = null)
    : AdsefidException(message, innerException);
