using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateParameterTypeJsonConverter : JsonConverter<TemplateParameterType>
{
    public override TemplateParameterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("TemplateParameterType must be a string."));

    public override void Write(Utf8JsonWriter writer, TemplateParameterType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value ?? throw new JsonException("TemplateParameterType has no value."));
}
