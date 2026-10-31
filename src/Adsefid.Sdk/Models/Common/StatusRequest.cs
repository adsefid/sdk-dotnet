using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Models.Common;

internal sealed class StatusRequest
{
    [JsonPropertyName("message_ids")]
    public IReadOnlyCollection<Guid>? MessageIds { get; init; }

    [JsonPropertyName("local_ids")]
    public IReadOnlyCollection<string>? LocalIds { get; init; }
}
