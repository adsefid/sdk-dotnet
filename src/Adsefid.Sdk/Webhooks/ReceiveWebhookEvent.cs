using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Webhooks;

/// <summary>Fired for <see cref="WebhookEventTypes.Receive"/>: one or more inbound SMS messages were received.</summary>
public sealed record ReceiveWebhookEvent : WebhookEvent
{
    /// <summary>The inbound messages carried by this delivery.</summary>
    public required IReadOnlyList<ReceivedMessageItem> Data { get; init; }
}

/// <summary>A single inbound SMS message.</summary>
public sealed class ReceivedMessageItem
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    /// <summary>The phone number the message was sent from.</summary>
    [JsonPropertyName("sender")]
    public required string Sender { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("receive_date")]
    public required DateTimeOffset ReceiveDate { get; init; }
}
