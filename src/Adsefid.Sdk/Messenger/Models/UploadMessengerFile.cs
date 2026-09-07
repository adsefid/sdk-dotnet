using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Messenger.Models;

/// <summary>Response from <see cref="MessengerResource.UploadFileAsync"/>.</summary>
public sealed class UploadMessengerFileResponse
{
    /// <summary>Pass this as <c>FileId</c> on a Messenger send to attach the uploaded file.</summary>
    [JsonPropertyName("file_id")]
    public required Guid FileId { get; init; }
}
