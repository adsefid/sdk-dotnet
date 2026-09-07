using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Models.Common;

/// <summary>The raw <c>error</c> object shape of an adsefid.com API error response. Consumers typically interact with <see cref="Exceptions.AdsefidApiException"/> instead, which is constructed from this.</summary>
public sealed class ApiErrorPayload
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("details")]
    public JsonElement? Details { get; init; }
}

/// <summary>The raw top-level shape of an adsefid.com API error response.</summary>
public sealed class ErrorEnvelope
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("error")]
    public ApiErrorPayload? Error { get; init; }
}
