using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateParameterTypeJsonConverter : JsonConverter<TemplateParameterType>
{
    /// <summary>
    /// Maps a wire value onto the documented set, returning <see langword="null"/> for anything
    /// else. The service is known to emit an undocumented third value, and callers cannot switch on
    /// what this SDK does not model, so an unrecognized type is dropped rather than surfaced.
    /// </summary>
    internal static TemplateParameterType? TryParse(string? wireValue) => wireValue switch
    {
        "string" => TemplateParameterType.String,
        "number" => TemplateParameterType.Number,
        _ => null,
    };

    internal static string ToWireValue(TemplateParameterType value) => value switch
    {
        TemplateParameterType.String => "string",
        TemplateParameterType.Number => "number",
        _ => throw new JsonException($"Unknown TemplateParameterType value '{value}'."),
    };

    public override TemplateParameterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TryParse(reader.GetString())
        ?? throw new JsonException($"Unknown TemplateParameterType value '{reader.GetString()}'.");

    public override void Write(Utf8JsonWriter writer, TemplateParameterType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToWireValue(value));
}
