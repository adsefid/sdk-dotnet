using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Messenger.Models;

/// <summary>Response from <see cref="MessengerResource.GetStatusAsync"/>.</summary>
public sealed class GetMessengerStatusResponse
{
    [JsonPropertyName("receptors")]
    public required IReadOnlyList<MessengerStatusItem> Receptors { get; init; }
}

/// <summary>The current status of a single previously sent Messenger message.</summary>
public sealed class MessengerStatusItem
{
    [JsonPropertyName("message_id")]
    public required Guid MessageId { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("status")]
    public required WebServiceMessageStatus Status { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    [JsonPropertyName("delivery_time")]
    public DateTimeOffset? DeliveryTime { get; init; }
}
