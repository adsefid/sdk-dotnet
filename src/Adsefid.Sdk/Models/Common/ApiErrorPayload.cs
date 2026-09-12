using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Models.Common;

/// <summary>The raw <c>error</c> object shape of an adsefid.com API error response. Consumers typically interact with <see cref="Exceptions.AdsefidApiException"/> instead, which is constructed from this.</summary>
public sealed class ApiErrorPayload
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("details")]
    public ApiErrorDetails? Details { get; init; }
}

/// <summary>Structured details attached to an API error.</summary>
public sealed class ApiErrorDetails
{
    /// <summary>Errors keyed by the request field name or item identifier that failed.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyDictionary<string, ApiFieldError>? Errors { get; init; }

    /// <summary>Errors for rejected bulk or P2P entries, indexed against the caller's input array.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<ApiItemError>? Items { get; init; }
}

/// <summary>A machine-readable error assigned to one request field.</summary>
public sealed class ApiFieldError
{
    /// <summary>The API error code. The value may be unnamed when the server adds a new code.</summary>
    [JsonPropertyName("code")]
    public required WebServiceResponseCode Code { get; init; }

    /// <summary>The uppercase symbolic error name returned by the API.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

/// <summary>Machine-readable field errors for one rejected bulk or P2P entry.</summary>
public sealed class ApiItemError
{
    /// <summary>The zero-based index of the rejected entry in the caller's input array.</summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>Errors keyed by field name for this rejected entry.</summary>
    [JsonPropertyName("errors")]
    public required IReadOnlyDictionary<string, ApiFieldError> Errors { get; init; }
}

/// <summary>The raw top-level shape of an adsefid.com API error response.</summary>
public sealed class ErrorEnvelope
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("error")]
    public ApiErrorPayload? Error { get; init; }
}
