using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Request for <see cref="SmsResource.CancelAsync"/>. Requires at least one id list, with at most 2000 distinct ids combined.</summary>
public sealed class CancelSmsRequest
{
    [JsonPropertyName("message_ids")]
    public IReadOnlyCollection<Guid>? MessageIds { get; init; }

    [JsonPropertyName("local_ids")]
    public IReadOnlyCollection<string>? LocalIds { get; init; }
}

/// <summary>Response from <see cref="SmsResource.CancelAsync"/>.</summary>
public sealed class CancelSmsResponse
{
    [JsonPropertyName("cancelled_messages")]
    public required IReadOnlyList<CancelledSmsItem> CancelledMessages { get; init; }

    /// <summary>Messages that could not be cancelled (e.g. already sent).</summary>
    [JsonPropertyName("failed_to_cancel")]
    public required IReadOnlyList<CancelledSmsItem> FailedToCancel { get; init; }
}

/// <summary>A single message's outcome in a cancel request.</summary>
public sealed class CancelledSmsItem
{
    /// <summary>Null when a requested local ID matches no message.</summary>
    [JsonPropertyName("message_id")]
    public required Guid? MessageId { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    /// <summary>The raw message status or per-identifier error code.</summary>
    [JsonPropertyName("status")]
    public required int Status { get; init; }

    /// <summary>The named message status, or null for an error or unknown status.</summary>
    [JsonIgnore]
    public WebServiceMessageStatus? MessageStatus => WebServiceCode.AsMessageStatus(Status);

    /// <summary>The named error for an unmatched identifier, or null for a message status or unknown error.</summary>
    [JsonIgnore]
    public WebServiceResponseCode? ErrorCode => WebServiceCode.AsErrorCode(Status);
}
