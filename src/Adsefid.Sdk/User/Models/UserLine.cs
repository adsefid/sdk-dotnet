using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.User.Models;

/// <summary>An SMS line available to the authenticated account, as returned by <see cref="UserResource.GetLinesAsync"/>. Pass <see cref="LineNumber"/> as a send request's line number.</summary>
public sealed class UserLine
{
    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    /// <summary>The tariff/billing mode this line uses by default.</summary>
    [JsonPropertyName("line_selector")]
    public required LineSelector LineSelector { get; init; }

    [JsonPropertyName("line_name")]
    public required string LineName { get; init; }

    /// <summary>Whether this line can currently be used to send messages.</summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }
}
