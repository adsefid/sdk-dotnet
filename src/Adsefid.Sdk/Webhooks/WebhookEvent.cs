using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Webhooks;

/// <summary>
/// Base type for a verified, parsed webhook delivery. <see cref="WebhookVerifier.VerifyAndParse(ReadOnlySpan{byte}, string, string, string, TimeSpan?)"/>
/// returns one of its concrete subtypes: <see cref="ReceiveWebhookEvent"/>,
/// <see cref="StatusWebhookEvent"/>, or <see cref="MessengerStatusWebhookEvent"/> — pattern-match on
/// the returned instance rather than switching on <see cref="Type"/> directly.
/// </summary>
public abstract record WebhookEvent
{
    /// <summary>Unique id of this webhook delivery.</summary>
    public required Guid Id { get; init; }

    /// <summary>The event type string (see <see cref="WebhookEventTypes"/>), matching the concrete subtype of this instance.</summary>
    public required string Type { get; init; }

    /// <summary>When the underlying event occurred, as reported by the server.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The delivery attempt number for this webhook (starts at 1; increases on retry).</summary>
    public required int Attempt { get; init; }

    /// <summary>The webhook payload schema version.</summary>
    public required string Version { get; init; }
}

internal sealed class RawWebhookEnvelope
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("occurred_at")]
    public required DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("attempt")]
    public required int Attempt { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("data")]
    public required JsonElement Data { get; init; }
}
