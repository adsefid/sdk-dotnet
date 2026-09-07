using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Models.Common;

namespace Adsefid.Sdk.Json;

internal sealed class TemplateParameterValueJsonConverter : JsonConverter<TemplateParameterValue>
{
    public override TemplateParameterValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TemplateParameterValue.ReadFrom(ref reader);

    public override void Write(Utf8JsonWriter writer, TemplateParameterValue value, JsonSerializerOptions options) =>
        value.WriteTo(writer);
}
