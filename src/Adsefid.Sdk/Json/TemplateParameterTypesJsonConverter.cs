using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Enums;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateParameterTypesJsonConverter : JsonConverter<IReadOnlyDictionary<string, TemplateParameterType>>
{
    public override IReadOnlyDictionary<string, TemplateParameterType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Template parameters must be an object.");
        }

        var result = new Dictionary<string, TemplateParameterType>(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Template parameter name was expected.");
            }

            var name = reader.GetString()!;
            if (!reader.Read())
            {
                throw new JsonException("Template parameter type was expected.");
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                continue;
            }

            // Drop a parameter whose declared type this SDK does not model, so
            // the dictionary never holds a value callers cannot switch on.
            if (TemplateParameterTypeJsonConverter.TryParse(reader.GetString()) is { } parameterType)
            {
                result[name] = parameterType;
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, TemplateParameterType> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, type) in value)
        {
            writer.WriteString(name, TemplateParameterTypeJsonConverter.ToWireValue(type));
        }

        writer.WriteEndObject();
    }
}
