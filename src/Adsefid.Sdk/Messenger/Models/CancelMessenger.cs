using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Messenger.Models;

/// <summary>Request for <see cref="MessengerResource.CancelAsync"/>. At least one of <see cref="MessageIds"/> or <see cref="LocalIds"/> is required; only messages that haven't been sent yet (e.g. scheduled ones) can be cancelled.</summary>
public sealed class CancelMessengerRequest
{
    [JsonPropertyName("message_ids")]
    public IReadOnlyCollection<Guid>? MessageIds { get; init; }

    [JsonPropertyName("local_ids")]
    public IReadOnlyCollection<string>? LocalIds { get; init; }
}

/// <summary>Response from <see cref="MessengerResource.CancelAsync"/>.</summary>
public sealed class CancelMessengerResponse
{
    [JsonPropertyName("cancelled_messages")]
    public required IReadOnlyList<CancelledMessengerItem> CancelledMessages { get; init; }

    /// <summary>Messages that could not be cancelled (e.g. already sent).</summary>
    [JsonPropertyName("failed_to_cancel")]
    public required IReadOnlyList<CancelledMessengerItem> FailedToCancel { get; init; }
}

/// <summary>A single message's outcome in a cancel request.</summary>
public sealed class CancelledMessengerItem
{
    [JsonPropertyName("message_id")]
    public required Guid MessageId { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("status")]
    public required WebServiceMessageStatus Status { get; init; }
}
