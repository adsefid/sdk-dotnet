using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Webhooks;

/// <summary>Fired for <see cref="WebhookEventTypes.Status"/>: one or more SMS messages' delivery status changed.</summary>
public sealed record StatusWebhookEvent : WebhookEvent
{
    /// <summary>The status updates carried by this delivery.</summary>
    public required IReadOnlyList<StatusUpdateItem> Data { get; init; }
}

/// <summary>A single message's delivery status update, for either an SMS or a Messenger message depending on which webhook event carries it.</summary>
public sealed class StatusUpdateItem
{
    /// <summary>The id of the message this update is for (matches <c>message_id</c> from the corresponding send/status response).</summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("status_delivery")]
    public required WebServiceMessageStatus StatusDelivery { get; init; }

    [JsonPropertyName("delivery_time")]
    public DateTimeOffset? DeliveryTime { get; init; }
}
