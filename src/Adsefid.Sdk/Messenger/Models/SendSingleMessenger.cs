using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Messenger.Models;

/// <summary>Request for <see cref="MessengerResource.SendSingleAsync"/>.</summary>
public sealed class SendSingleMessengerRequest
{
    /// <summary>The message text. Must be at most 4000 characters — checked client-side before any network call.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>The Messenger profile to send from (see <see cref="User.UserResource.GetProfilesAsync"/>).</summary>
    [JsonPropertyName("profile")]
    public required Guid Profile { get; init; }

    [JsonPropertyName("hide")]
    public bool? Hide { get; init; }

    /// <summary>Id of a file previously uploaded via <see cref="MessengerResource.UploadFileAsync"/>, to attach to this message.</summary>
    [JsonPropertyName("file_id")]
    public Guid? FileId { get; init; }

    /// <summary>Schedule the message for future delivery at this time instead of sending immediately.</summary>
    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    /// <summary>An optional correlation id you supply and get back unchanged. Must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.</summary>
    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }
}

/// <summary>Response from <see cref="MessengerResource.SendSingleAsync"/>.</summary>
public sealed class SendSingleMessengerResponse
{
    /// <summary>Id of the send request that created this message. For a single send this identifies just this one message; bulk/P2P sends share one <see cref="GroupId"/> across all their messages.</summary>
    [JsonPropertyName("group_id")]
    public required Guid GroupId { get; init; }

    [JsonPropertyName("message_id")]
    public required Guid MessageId { get; init; }

    [JsonPropertyName("status")]
    public required WebServiceMessageStatus Status { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("hide")]
    public required bool Hide { get; init; }

    [JsonPropertyName("cost")]
    public required long Cost { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    [JsonPropertyName("profile")]
    public required Guid Profile { get; init; }

    /// <summary>The messenger platform this profile sends through (e.g. <c>"whatsapp"</c>, <c>"telegram"</c>).</summary>
    [JsonPropertyName("messenger")]
    public required string Messenger { get; init; }
}
