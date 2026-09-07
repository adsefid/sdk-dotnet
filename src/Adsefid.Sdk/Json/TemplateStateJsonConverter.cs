using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateStateJsonConverter : JsonConverter<TemplateState>
{
    public override TemplateState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "pendingapproval" => TemplateState.PendingApproval,
            "approved" => TemplateState.Approved,
            "rejected" => TemplateState.Rejected,
            var value => throw new JsonException($"Unknown TemplateState value '{value}'."),
        };

    public override void Write(Utf8JsonWriter writer, TemplateState value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            TemplateState.PendingApproval => "pendingapproval",
            TemplateState.Approved => "approved",
            TemplateState.Rejected => "rejected",
            _ => throw new JsonException($"Unknown TemplateState value '{value}'."),
        });
}
