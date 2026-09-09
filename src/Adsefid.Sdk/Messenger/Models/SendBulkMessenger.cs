using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Messenger.Models;

/// <summary>Request for <see cref="MessengerResource.SendBulkAsync"/>: the same message sent to every receptor.</summary>
public sealed class SendBulkMessengerRequest
{
    [JsonPropertyName("receptors")]
    public required IReadOnlyList<BulkMessengerReceptor> Receptors { get; init; }

    /// <summary>The message text, sent identically to every receptor. Must be at most 4000 characters — checked client-side before any network call.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>The Messenger profile to send from (see <see cref="User.UserResource.GetProfilesAsync"/>).</summary>
    [JsonPropertyName("profile")]
    public required Guid Profile { get; init; }

    /// <summary>Id of a file previously uploaded via <see cref="MessengerResource.UploadFileAsync"/>, to attach to every message.</summary>
    [JsonPropertyName("file_id")]
    public Guid? FileId { get; init; }
}

/// <summary>One receptor entry of a bulk Messenger send.</summary>
public sealed class BulkMessengerReceptor
{
    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>An optional correlation id you supply and get back unchanged for this receptor. Must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.</summary>
    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("hide")]
    public bool? Hide { get; init; }
}

/// <summary>
/// Response from <see cref="MessengerResource.SendBulkAsync"/>. An HTTP success here does not mean
/// every receptor succeeded — check each item's <see cref="BulkMessengerReceptorResult.Status"/>.
/// </summary>
public sealed class SendBulkMessengerResponse
{
    /// <summary>Id shared by every message created from this bulk send.</summary>
    [JsonPropertyName("group_id")]
    public required Guid GroupId { get; init; }

    /// <summary>Per-receptor results, in the same order as the request's <see cref="SendBulkMessengerRequest.Receptors"/>.</summary>
    [JsonPropertyName("receptors")]
    public required IReadOnlyList<BulkMessengerReceptorResult> Receptors { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>Total number of receptors in this send.</summary>
    [JsonPropertyName("total_count")]
    public required int TotalCount { get; init; }

    /// <summary>Total cost charged across all receptors in this send.</summary>
    [JsonPropertyName("total_cost")]
    public required decimal TotalCost { get; init; }

    /// <summary>Breakdown of receptor counts by outcome.</summary>
    [JsonPropertyName("counts")]
    public required IReadOnlyDictionary<string, int> Counts { get; init; }

    [JsonPropertyName("profile")]
    public required Guid Profile { get; init; }

    /// <summary>The messenger platform this profile sends through (e.g. <c>"whatsapp"</c>, <c>"telegram"</c>).</summary>
    [JsonPropertyName("messenger")]
    public required string Messenger { get; init; }
}

/// <summary>The outcome for a single receptor in a bulk Messenger send.</summary>
public sealed class BulkMessengerReceptorResult
{
    /// <summary><see langword="null"/> if this receptor failed before a message could be created.</summary>
    [JsonPropertyName("message_id")]
    public Guid? MessageId { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("hide")]
    public required bool Hide { get; init; }

    /// <summary>
    /// The raw per-item <c>WebServiceCode</c>: 1000-1999 means this item was accepted (see
    /// <see cref="MessageStatus"/>), 2000 or above means this one item was rejected (see
    /// <see cref="ErrorCode"/>). Kept as an <see cref="int"/> so a code this SDK does not know yet
    /// still round-trips.
    /// </summary>
    [JsonPropertyName("status")]
    public required int Status { get; init; }

    /// <summary>The named message status when <see cref="Status"/> is in 1000-1999, otherwise <see langword="null"/>.</summary>
    [JsonIgnore]
    public WebServiceMessageStatus? MessageStatus => WebServiceCode.AsMessageStatus(Status);

    /// <summary>The named error code when <see cref="Status"/> is 2000 or above, otherwise <see langword="null"/>.</summary>
    [JsonIgnore]
    public WebServiceResponseCode? ErrorCode => WebServiceCode.AsErrorCode(Status);

    [JsonPropertyName("cost")]
    public required decimal Cost { get; init; }
}
