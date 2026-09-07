using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.User.Models;

/// <summary>Response from <see cref="UserResource.GetTemplatesAsync"/>.</summary>
public sealed class GetUserTemplatesResponse
{
    /// <summary>The page of templates matching the request's filters.</summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<UserTemplate> Items { get; init; }

    /// <summary>Total number of templates matching the request's filters, across all pages.</summary>
    [JsonPropertyName("total")]
    public required int Total { get; init; }
}

/// <summary>An account SMS/Messenger template. Use <see cref="TemplateId"/> with <c>SendTemplateAsync</c> once <see cref="State"/> is <see cref="TemplateState.Approved"/>.</summary>
public sealed class UserTemplate
{
    [JsonPropertyName("template_id")]
    public required string TemplateId { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>The template's named parameters and their expected value type, keyed by parameter name.</summary>
    [JsonPropertyName("parameters")]
    [JsonConverter(typeof(Adsefid.Sdk.Json.TemplateParameterTypesJsonConverter))]
    public required IReadOnlyDictionary<string, TemplateParameterType> Parameters { get; init; }

    [JsonPropertyName("state")]
    public required TemplateState State { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}
