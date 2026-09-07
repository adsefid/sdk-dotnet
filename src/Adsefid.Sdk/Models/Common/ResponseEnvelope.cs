using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Models.Common;

/// <summary>The raw top-level shape of a successful adsefid.com API response. Resource methods unwrap this and return <typeparamref name="TData"/> directly.</summary>
public sealed class ResponseEnvelope<TData>
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("data")]
    public TData? Data { get; init; }
}
