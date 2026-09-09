namespace Adsefid.Sdk.Enums;

/// <summary>
/// Helpers for a per-item <c>status</c> value in a bulk or P2P send response. The service reports
/// a <c>WebServiceCode</c> there: a value in 1000-1999 is a <see cref="WebServiceMessageStatus"/>
/// (the item was accepted), and a value of 2000 or above is a <see cref="WebServiceResponseCode"/>
/// (that one item was rejected, even though the overall response is a success).
/// </summary>
public static class WebServiceCode
{
    private const int MessageStatusMin = 1000;
    private const int ErrorCodeMin = 2000;

    /// <summary>Whether <paramref name="code"/> falls in the message-status range (1000-1999).</summary>
    public static bool IsMessageStatus(int code) => code >= MessageStatusMin && code < ErrorCodeMin;

    /// <summary>Whether <paramref name="code"/> falls in the error-code range (2000 and above).</summary>
    public static bool IsErrorCode(int code) => code >= ErrorCodeMin;

    /// <summary>The named <see cref="WebServiceMessageStatus"/> for <paramref name="code"/>, or <see langword="null"/> when it is an error code or a status this SDK does not know yet.</summary>
    public static WebServiceMessageStatus? AsMessageStatus(int code) =>
        IsMessageStatus(code) && Enum.IsDefined(typeof(WebServiceMessageStatus), code)
            ? (WebServiceMessageStatus)code
            : null;

    /// <summary>The named <see cref="WebServiceResponseCode"/> for <paramref name="code"/>, or <see langword="null"/> when it is a message status or an error code this SDK does not know yet.</summary>
    public static WebServiceResponseCode? AsErrorCode(int code) =>
        IsErrorCode(code) && Enum.IsDefined(typeof(WebServiceResponseCode), code)
            ? (WebServiceResponseCode)code
            : null;
}
