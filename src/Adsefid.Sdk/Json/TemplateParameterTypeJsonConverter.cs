using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateParameterTypeJsonConverter : JsonConverter<TemplateParameterType>
{
    public override TemplateParameterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "string" => TemplateParameterType.String,
            "number" => TemplateParameterType.Number,
            var value => throw new JsonException($"Unknown TemplateParameterType value '{value}'."),
        };

    public override void Write(Utf8JsonWriter writer, TemplateParameterType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            TemplateParameterType.String => "string",
            TemplateParameterType.Number => "number",
            _ => throw new JsonException($"Unknown TemplateParameterType value '{value}'."),
        });
}
