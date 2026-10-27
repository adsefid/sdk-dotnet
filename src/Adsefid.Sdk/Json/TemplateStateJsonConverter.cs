using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateStateJsonConverter : JsonConverter<TemplateState>
{
    public override TemplateState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("TemplateState must be a string."));

    public override void Write(Utf8JsonWriter writer, TemplateState value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value ?? throw new JsonException("TemplateState has no value."));
}
