using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Enums;

/// <summary>The moderation state of an SMS/Messenger template. Only <see cref="Approved"/> templates can be used to send messages.</summary>
[JsonConverter(typeof(TemplateStateJsonConverter))]
public enum TemplateState
{
    PendingApproval,
    Approved,
    Rejected,
}
