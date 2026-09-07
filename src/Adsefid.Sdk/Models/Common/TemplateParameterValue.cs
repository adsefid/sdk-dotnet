using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Models.Common;

/// <summary>
/// A template parameter value, serialized as either a JSON string or a JSON number — never both.
/// Assign a <see cref="string"/>, <see cref="int"/>, or <see cref="double"/> directly via the
/// implicit conversions rather than constructing this manually.
/// </summary>
[JsonConverter(typeof(TemplateParameterValueJsonConverter))]
public readonly struct TemplateParameterValue
{
    private readonly string? _stringValue;
    private readonly double? _numberValue;

    private TemplateParameterValue(string? stringValue, double? numberValue)
    {
        _stringValue = stringValue;
        _numberValue = numberValue;
    }

    /// <summary>Whether this value holds a number (<see langword="true"/>) or a string (<see langword="false"/>). Check this, or use <see cref="AsString"/>/<see cref="AsNumber"/>, before reading the value.</summary>
    public bool IsNumber => _numberValue.HasValue;

    /// <summary>The value as a string, or <see langword="null"/> if <see cref="IsNumber"/> is <see langword="true"/>.</summary>
    public string? AsString => _stringValue;

    /// <summary>The value as a number, or <see langword="null"/> if <see cref="IsNumber"/> is <see langword="false"/>.</summary>
    public double? AsNumber => _numberValue;

    /// <summary>Creates a string-valued parameter.</summary>
    public static TemplateParameterValue FromString(string value) => new(value, null);

    /// <summary>Creates a number-valued parameter.</summary>
    public static TemplateParameterValue FromNumber(double value) => new(null, value);

    public static implicit operator TemplateParameterValue(string value) => FromString(value);

    public static implicit operator TemplateParameterValue(int value) => FromNumber(value);

    public static implicit operator TemplateParameterValue(double value) => FromNumber(value);

    internal static TemplateParameterValue ReadFrom(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => FromString(reader.GetString()!),
        JsonTokenType.Number => FromNumber(reader.GetDouble()),
        _ => throw new JsonException("A template parameter value must be a JSON string or number."),
    };

    internal void WriteTo(Utf8JsonWriter writer)
    {
        if (_numberValue.HasValue)
        {
            writer.WriteNumberValue(_numberValue.Value);
        }
        else
        {
            writer.WriteStringValue(_stringValue);
        }
    }
}
