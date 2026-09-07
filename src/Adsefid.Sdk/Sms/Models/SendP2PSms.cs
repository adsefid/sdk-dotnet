using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Request for <see cref="SmsResource.SendP2PAsync"/>: a distinct message per receptor, sent in one call.</summary>
public sealed class SendP2PSmsRequest
{
    [JsonPropertyName("messages")]
    public required IReadOnlyList<P2PSmsMessage> Messages { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("line_selector")]
    public LineSelector? LineSelector { get; init; }
}

public sealed class P2PSmsMessage
{
    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>The message text for this receptor. Must be at most 900 characters — checked client-side before any network call.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>An optional correlation id you supply and get back unchanged for this message. Must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.</summary>
    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("hide")]
    public bool? Hide { get; init; }
}

/// <summary>
/// Response from <see cref="SmsResource.SendP2PAsync"/>. An HTTP success here does not mean every
/// message succeeded — check each item's <see cref="P2PSmsMessageResult.Status"/>.
/// </summary>
public sealed class SendP2PSmsResponse
{
    /// <summary>Id shared by every message created from this P2P send.</summary>
    [JsonPropertyName("group_id")]
    public required Guid GroupId { get; init; }

    /// <summary>Per-message results, in the same order as the request's <see cref="SendP2PSmsRequest.Messages"/>.</summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<P2PSmsMessageResult> Messages { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("line_selector")]
    public LineSelector LineSelector { get; init; }

    /// <summary>Total cost charged across all messages in this send.</summary>
    [JsonPropertyName("total_cost")]
    public required long TotalCost { get; init; }

    /// <summary>Breakdown of message counts by outcome.</summary>
    [JsonPropertyName("counts")]
    public required IReadOnlyDictionary<string, int> Counts { get; init; }
}

/// <summary>The outcome for a single message in a P2P SMS send.</summary>
public sealed class P2PSmsMessageResult
{
    /// <summary><see langword="null"/> if this message failed before it could be created.</summary>
    [JsonPropertyName("message_id")]
    public Guid? MessageId { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>Numeric per-item status code (see <see cref="WebServiceMessageStatus"/> for known values).</summary>
    [JsonPropertyName("status")]
    public required int Status { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("hide")]
    public required bool Hide { get; init; }

    /// <summary>Number of SMS segments this message was split into for billing/delivery purposes.</summary>
    [JsonPropertyName("segment_count")]
    public required int SegmentCount { get; init; }

    [JsonPropertyName("cost")]
    public required long Cost { get; init; }
}
