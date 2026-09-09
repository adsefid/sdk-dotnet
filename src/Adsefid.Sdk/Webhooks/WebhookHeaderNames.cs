namespace Adsefid.Sdk.Webhooks;

/// <summary>HTTP header names used on outgoing webhook requests, so you never have to type them as string literals.</summary>
public static class WebhookHeaderNames
{
    /// <summary>Header carrying the webhook delivery id (matches <see cref="WebhookEvent.Id"/>).</summary>
    public const string Id = "X-Atlas-Webhook-Id";

    /// <summary>Header carrying the request signature, passed as <c>signatureHeader</c> to <see cref="WebhookVerifier.VerifyAndParse(ReadOnlySpan{byte}, string, string, string, TimeSpan?)"/>.</summary>
    public const string Signature = "X-Atlas-Webhook-Signature";

    /// <summary>Header carrying the request's Unix timestamp, passed as <c>timestampHeader</c> to <see cref="WebhookVerifier.VerifyAndParse(ReadOnlySpan{byte}, string, string, string, TimeSpan?)"/>.</summary>
    public const string Timestamp = "X-Atlas-Webhook-Timestamp";

    /// <summary>Header carrying the event type string (see <see cref="WebhookEventTypes"/>).</summary>
    public const string Event = "X-Atlas-Webhook-Event";

    /// <summary>Header carrying the delivery attempt number (matches <see cref="WebhookEvent.Attempt"/>).</summary>
    public const string Attempt = "X-Atlas-Webhook-Attempt";
}
