using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Models.Common;

/// <summary>
/// A template parameter value, serialized as either a JSON string or a JSON number — never both.
/// Assign a <see cref="string"/>, <see cref="int"/>, <see cref="long"/>, <see cref="decimal"/>, or
/// <see cref="double"/> directly via the implicit conversions rather than constructing this manually.
/// </summary>
/// <remarks>
/// For a parameter the template declares as <c>number</c>, pass a <see cref="string"/> whenever the
/// exact digits matter: the service substitutes a numeric string verbatim, so <c>"001234"</c> keeps
/// its leading zeros and <c>"1.50"</c> its trailing zero, where the numbers <c>1234</c> and
/// <c>1.5</c> would not. Numbers are held as <see cref="decimal"/> so an ordinary decimal value
/// round-trips exactly; <see cref="double"/> is accepted for convenience and converted.
/// </remarks>
[JsonConverter(typeof(TemplateParameterValueJsonConverter))]
public readonly struct TemplateParameterValue
{
    private enum ValueKind : byte
    {
        /// <summary>The default, uninitialized state — not a valid parameter value.</summary>
        None = 0,
        String,
        Number,
    }

    private readonly ValueKind _kind;
    private readonly string? _stringValue;
    private readonly decimal _numberValue;

    private TemplateParameterValue(ValueKind kind, string? stringValue, decimal numberValue)
    {
        _kind = kind;
        _stringValue = stringValue;
        _numberValue = numberValue;
    }

    /// <summary>Whether this value holds a number (<see langword="true"/>) or a string (<see langword="false"/>). Check this, or use <see cref="AsString"/>/<see cref="AsNumber"/>, before reading the value.</summary>
    public bool IsNumber => _kind == ValueKind.Number;

    /// <summary>Whether this value was ever assigned. A <see langword="default"/> instance is not a usable parameter value and cannot be serialized.</summary>
    public bool HasValue => _kind != ValueKind.None;

    /// <summary>The value as a string, or <see langword="null"/> if this value holds a number or was never assigned.</summary>
    public string? AsString => _kind == ValueKind.String ? _stringValue : null;

    /// <summary>The value as a number, or <see langword="null"/> if this value holds a string or was never assigned.</summary>
    public decimal? AsNumber => _kind == ValueKind.Number ? _numberValue : null;

    /// <summary>Creates a string-valued parameter.</summary>
    public static TemplateParameterValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new TemplateParameterValue(ValueKind.String, value, default);
    }

    /// <summary>Creates a number-valued parameter.</summary>
    public static TemplateParameterValue FromNumber(decimal value) =>
        new(ValueKind.Number, null, value);

    public static implicit operator TemplateParameterValue(string value) => FromString(value);

    public static implicit operator TemplateParameterValue(int value) => FromNumber(value);

    public static implicit operator TemplateParameterValue(long value) => FromNumber(value);

    public static implicit operator TemplateParameterValue(decimal value) => FromNumber(value);

    /// <summary>
    /// Converts a <see cref="double"/>, which cannot represent every decimal exactly. Prefer
    /// <see cref="decimal"/>, or a string when the exact digits matter.
    /// </summary>
    public static implicit operator TemplateParameterValue(double value) => FromNumber((decimal)value);

    /// <inheritdoc />
    public override string ToString() => _kind switch
    {
        ValueKind.String => _stringValue!,
        ValueKind.Number => _numberValue.ToString(CultureInfo.InvariantCulture),
        _ => string.Empty,
    };

    internal static TemplateParameterValue ReadFrom(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => FromString(reader.GetString()!),
        // A number too large or precise for decimal still round-trips as a double.
        JsonTokenType.Number => FromNumber(reader.TryGetDecimal(out var value) ? value : (decimal)reader.GetDouble()),
        _ => throw new JsonException("A template parameter value must be a JSON string or number."),
    };

    internal void WriteTo(Utf8JsonWriter writer)
    {
        switch (_kind)
        {
            case ValueKind.Number:
                writer.WriteNumberValue(_numberValue);
                break;
            case ValueKind.String:
                writer.WriteStringValue(_stringValue);
                break;
            default:
                // Serializing a default(TemplateParameterValue) would emit JSON
                // null, which the service rejects and this type's own reader
                // would refuse. Fail here, where the cause is still visible.
                throw new JsonException(
                    "A template parameter value was never assigned. Assign a string or a number rather than leaving it at its default.");
        }
    }
}
