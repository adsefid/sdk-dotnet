using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Adsefid.Sdk.Tests.Infrastructure;

/// <summary>
/// Reproduces the service's webhook signing, for tests needing a fresh signature rather than the
/// fixed golden vector.
/// </summary>
internal static class WebhookSigner
{
    public const string Prefix = "v1=";

    /// <summary>
    /// HMAC-SHA256 over <c>"{timestamp}.{rawBody}"</c>, keyed with the DECODED secret. The panel
    /// shows the secret Base64-encoded; the key is what it decodes to.
    /// </summary>
    public static string Sign(string secretBase64, string timestamp, byte[] rawBody)
    {
        var key = Convert.FromBase64String(secretBase64);
        var input = Encoding.UTF8.GetBytes(timestamp + ".").Concat(rawBody).ToArray();
        return Prefix + Convert.ToBase64String(HMACSHA256.HashData(key, input));
    }

    public static string Now(int offsetSeconds = 0) =>
        DateTimeOffset.UtcNow.AddSeconds(offsetSeconds).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
}
