using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Request for <see cref="SmsResource.SendSingleAsync"/>.</summary>
public sealed class SendSingleSmsRequest
{
    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>The sender line number assigned to your account (see <see cref="User.UserResource.GetLinesAsync"/>).</summary>
    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    /// <summary>Which tariff/billing mode to send under. Omit to use the account's default for <see cref="LineNumber"/>.</summary>
    [JsonPropertyName("line_selector")]
    public LineSelector? LineSelector { get; init; }

    /// <summary>The message text. Must be at most 900 characters — checked client-side before any network call.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Schedule the message for future delivery at this time instead of sending immediately.</summary>
    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>An optional correlation id you supply and get back unchanged. Must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.</summary>
    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("hide")]
    public bool? Hide { get; init; }
}

/// <summary>Response from <see cref="SmsResource.SendSingleAsync"/>.</summary>
public sealed class SendSingleSmsResponse
{
    /// <summary>Id of the send request that created this message. For a single send this identifies just this one message; bulk/P2P sends share one <see cref="GroupId"/> across all their messages.</summary>
    [JsonPropertyName("group_id")]
    public required Guid GroupId { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("status")]
    public required WebServiceMessageStatus Status { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("line_selector")]
    public LineSelector LineSelector { get; init; }

    [JsonPropertyName("cost")]
    public required long Cost { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>Id of this specific message. Use this (not <see cref="GroupId"/>) to look up or cancel this message.</summary>
    [JsonPropertyName("message_id")]
    public required Guid MessageId { get; init; }

    /// <summary>Number of SMS segments this message was split into for billing/delivery purposes.</summary>
    [JsonPropertyName("segment_count")]
    public required int SegmentCount { get; init; }

    [JsonPropertyName("hide")]
    public required bool Hide { get; init; }
}
