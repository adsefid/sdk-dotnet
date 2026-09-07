using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Request for <see cref="SmsResource.SendTemplateAsync"/>: sends a pre-approved template with its parameters filled in. The template must be in <see cref="Enums.TemplateState.Approved"/> state (see <see cref="User.UserResource.GetTemplatesAsync"/>).</summary>
public sealed class SendTemplateSmsRequest
{
    [JsonPropertyName("template_id")]
    public required string TemplateId { get; init; }

    /// <summary>Values for the template's named parameters, keyed by parameter name. Each value must match the parameter's declared <see cref="Enums.TemplateParameterType"/> (string or number).</summary>
    [JsonPropertyName("parameters")]
    public required IReadOnlyDictionary<string, TemplateParameterValue> Parameters { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    /// <summary>An optional correlation id you supply and get back unchanged. Must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.</summary>
    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("line_selector")]
    public LineSelector? LineSelector { get; init; }

    /// <summary>If set, the message is not sent after this time.</summary>
    [JsonPropertyName("expiry_date")]
    public DateTimeOffset? ExpiryDate { get; init; }
}

/// <summary>Response from <see cref="SmsResource.SendTemplateAsync"/>.</summary>
public sealed class SendTemplateSmsResponse
{
    [JsonPropertyName("group_id")]
    public required Guid GroupId { get; init; }

    [JsonPropertyName("message_id")]
    public required Guid MessageId { get; init; }

    [JsonPropertyName("status")]
    public required WebServiceMessageStatus Status { get; init; }

    [JsonPropertyName("local_id")]
    public string? LocalId { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("template_id")]
    public required string TemplateId { get; init; }

    [JsonPropertyName("send_time")]
    public DateTimeOffset? SendTime { get; init; }

    [JsonPropertyName("expiry_date")]
    public DateTimeOffset? ExpiryDate { get; init; }

    [JsonPropertyName("line_selector")]
    public LineSelector LineSelector { get; init; }

    [JsonPropertyName("cost")]
    public required long Cost { get; init; }

    [JsonPropertyName("receptor")]
    public required string Receptor { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("segment_count")]
    public required int SegmentCount { get; init; }

    [JsonPropertyName("parameters")]
    public required IReadOnlyDictionary<string, TemplateParameterValue> Parameters { get; init; }
}
