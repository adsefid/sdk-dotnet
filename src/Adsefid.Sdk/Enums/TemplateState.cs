using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Enums;

/// <summary>
/// The moderation state of an SMS/Messenger template. Only <see cref="Approved"/> templates can be
/// used to send messages. A state this SDK does not know yet is kept as its raw
/// <see cref="Value"/> instead of failing to parse.
/// </summary>
[JsonConverter(typeof(TemplateStateJsonConverter))]
public readonly record struct TemplateState(string Value)
{
    public static readonly TemplateState PendingApproval = new("pendingapproval");

    public static readonly TemplateState Approved = new("approved");

    public static readonly TemplateState Rejected = new("rejected");

    /// <summary>The wire value, for example <c>"approved"</c>.</summary>
    public override string ToString() => Value ?? string.Empty;
}
