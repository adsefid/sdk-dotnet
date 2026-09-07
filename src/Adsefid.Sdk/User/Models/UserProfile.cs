using System.Text.Json.Serialization;

namespace Adsefid.Sdk.User.Models;

/// <summary>A Messenger profile available to the authenticated account, as returned by <see cref="UserResource.GetProfilesAsync"/>. Pass <see cref="Id"/> as a Messenger send request's profile.</summary>
public sealed class UserProfile
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The messenger platform this profile sends through (e.g. <c>"whatsapp"</c>, <c>"telegram"</c>).</summary>
    [JsonPropertyName("messenger")]
    public required string Messenger { get; init; }
}
