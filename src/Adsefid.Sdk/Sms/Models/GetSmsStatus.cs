using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Response from <see cref="SmsResource.GetStatusAsync"/>.</summary>
public sealed class GetSmsStatusResponse
{
    [JsonPropertyName("receptors")]
    public required IReadOnlyList<SmsStatusItem> Receptors { get; init; }
}

/// <summary>The current status of a single previously sent SMS message.</summary>
public sealed class SmsStatusItem
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
