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

            if (reader.TokenType == JsonTokenType.String)
            {
                switch (reader.GetString())
                {
                    case "string": result[name] = TemplateParameterType.String; break;
                    case "number": result[name] = TemplateParameterType.Number; break;
                }
            }
            else
            {
                reader.Skip();
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, TemplateParameterType> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, type) in value)
        {
            writer.WriteString(name, type switch
            {
                TemplateParameterType.String => "string",
                TemplateParameterType.Number => "number",
                _ => throw new JsonException($"Unknown TemplateParameterType value '{type}'."),
            });
        }
        writer.WriteEndObject();
    }
}
