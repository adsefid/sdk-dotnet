using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Webhooks;

/// <summary>Verifies and parses incoming adsefid.com webhook deliveries.</summary>
public static class WebhookVerifier
{
    private const string SignaturePrefix = "v1=";
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Verifies the signature and timestamp of a webhook request and parses its payload. The
    /// signature is HMAC-SHA256 over <c>"{timestamp}.{rawBody}"</c> using <paramref name="secret"/>,
    /// Base64-encoded directly (no hex intermediate step), and compared with
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals"/> for
    /// constant-time comparison.
    /// </summary>
    /// <param name="rawBody">The exact, unmodified request body bytes as received, decoded as a string — re-encoding or reformatting it before calling this method will break signature verification.</param>
    /// <param name="signatureHeader">The value of the <see cref="WebhookHeaderNames.Signature"/> header.</param>
    /// <param name="timestampHeader">The value of the <see cref="WebhookHeaderNames.Timestamp"/> header.</param>
    /// <param name="secret">
    /// Your webhook signing secret exactly as shown in your adsefid.com panel: the Base64 encoding
    /// of 32 random bytes. The service signs with those decoded bytes, so the value is Base64-decoded
    /// here before it is used as an HMAC key. Use the <see cref="ReadOnlySpan{T}"/> overload if you
    /// already hold the decoded key.
    /// </param>
    /// <param name="maxAge">Maximum allowed age of the timestamp. Defaults to 5 minutes.</param>
    /// <returns>
    /// The parsed event as one of <see cref="ReceiveWebhookEvent"/>, <see cref="StatusWebhookEvent"/>,
    /// or <see cref="MessengerStatusWebhookEvent"/>, depending on the payload's <c>type</c>.
    /// </returns>
    /// <exception cref="AdsefidWebhookVerificationException">
    /// The secret is not valid Base64; the timestamp is not a valid Unix timestamp, is older than
    /// <paramref name="maxAge"/>; the signature is missing its <c>"v1="</c> prefix or does not
    /// match; or the payload is not valid JSON in the expected shape, including an unrecognized
    /// <c>type</c>.
    /// </exception>
    public static WebhookEvent VerifyAndParse(
        string rawBody,
        string signatureHeader,
        string timestampHeader,
        string secret,
        TimeSpan? maxAge = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var key = new byte[((secret.Length + 3) / 4) * 3];
        if (!Convert.TryFromBase64String(secret.Trim(), key, out var keyLength) || keyLength == 0)
        {
            throw new AdsefidWebhookVerificationException(
                "The webhook secret is not valid Base64. Use the secret exactly as shown in your adsefid.com panel, or call the overload taking the decoded key.");
        }

        return VerifyAndParse(rawBody, signatureHeader, timestampHeader, key.AsSpan(0, keyLength), maxAge, cancellationToken);
    }

    /// <summary>
    /// Verifies and parses a webhook request using an already-decoded signing key, skipping the
    /// Base64 step. Use this when you store the decoded key yourself, for example in a secret store.
    /// </summary>
    /// <inheritdoc cref="VerifyAndParse(string, string, string, string, TimeSpan?, CancellationToken)" path="/param|/returns|/exception"/>
    public static WebhookEvent VerifyAndParse(
        string rawBody,
        string signatureHeader,
        string timestampHeader,
        ReadOnlySpan<byte> key,
        TimeSpan? maxAge = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(signatureHeader);
        ArgumentNullException.ThrowIfNull(timestampHeader);

        cancellationToken.ThrowIfCancellationRequested();

        if (!long.TryParse(timestampHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            throw new AdsefidWebhookVerificationException("The webhook timestamp header is not a valid Unix timestamp.");
        }

        if (!signatureHeader.StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            throw new AdsefidWebhookVerificationException("The webhook signature header is missing the 'v1=' prefix.");
        }

        var providedSignature = signatureHeader[SignaturePrefix.Length..];
        var expectedSignature = ComputeSignature(timestampSeconds, rawBody, key);

        if (!SignaturesMatch(providedSignature, expectedSignature))
        {
            throw new AdsefidWebhookVerificationException("The webhook signature is invalid.");
        }

        var effectiveMaxAge = maxAge ?? DefaultMaxAge;
        DateTimeOffset timestampAsOf;
        try
        {
            timestampAsOf = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new AdsefidWebhookVerificationException("The webhook timestamp is outside the supported range.");
        }

        if ((DateTimeOffset.UtcNow - timestampAsOf).Duration() > effectiveMaxAge)
        {
            throw new AdsefidWebhookVerificationException("The webhook timestamp is outside the allowed max age.");
        }

        RawWebhookEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(rawBody, AdsefidJsonContext.Default.RawWebhookEnvelope)
                ?? throw new AdsefidWebhookVerificationException("The webhook payload is empty or malformed.");
        }
        catch (JsonException)
        {
            throw new AdsefidWebhookVerificationException("The webhook payload could not be parsed as JSON.");
        }

        return envelope.Type switch
        {
            WebhookEventTypes.Receive => new ReceiveWebhookEvent
            {
                Id = envelope.Id,
                Type = envelope.Type,
                OccurredAt = envelope.OccurredAt,
                Attempt = envelope.Attempt,
                Version = envelope.Version,
                Data = DeserializeData(envelope.Data, AdsefidJsonContext.Default.ListReceivedMessageItem),
            },
            WebhookEventTypes.Status => new StatusWebhookEvent
            {
                Id = envelope.Id,
                Type = envelope.Type,
                OccurredAt = envelope.OccurredAt,
                Attempt = envelope.Attempt,
                Version = envelope.Version,
                Data = DeserializeData(envelope.Data, AdsefidJsonContext.Default.ListStatusUpdateItem),
            },
            WebhookEventTypes.MessengerStatus => new MessengerStatusWebhookEvent
            {
                Id = envelope.Id,
                Type = envelope.Type,
                OccurredAt = envelope.OccurredAt,
                Attempt = envelope.Attempt,
                Version = envelope.Version,
                Data = DeserializeData(envelope.Data, AdsefidJsonContext.Default.ListStatusUpdateItem),
            },
            _ => throw new AdsefidWebhookVerificationException($"Unsupported webhook event type '{envelope.Type}'."),
        };
    }

    private static List<T> DeserializeData<T>(JsonElement data, JsonTypeInfo<List<T>> typeInfo)
    {
        try
        {
            return data.Deserialize(typeInfo) ?? [];
        }
        catch (JsonException)
        {
            throw new AdsefidWebhookVerificationException("The webhook payload 'data' field could not be parsed.");
        }
    }

    private static string ComputeSignature(long timestampSeconds, string rawBody, ReadOnlySpan<byte> key)
    {
        var signingInput = $"{timestampSeconds.ToString(CultureInfo.InvariantCulture)}.{rawBody}";
        var inputBytes = Encoding.UTF8.GetBytes(signingInput);
        var hash = HMACSHA256.HashData(key, inputBytes);
        return Convert.ToBase64String(hash);
    }

    private static bool SignaturesMatch(string provided, string expected)
    {
        byte[] providedBytes;
        byte[] expectedBytes;
        try
        {
            providedBytes = Convert.FromBase64String(provided);
            expectedBytes = Convert.FromBase64String(expected);
        }
        catch (FormatException)
        {
            return false;
        }

        return providedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
