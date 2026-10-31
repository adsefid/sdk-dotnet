using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Enums;

/// <summary>
/// The declared type of one template parameter. The public named types are { string, number }.
/// A type this SDK does not know yet is preserved as its raw <see cref="Value"/> instead of
/// failing to parse.
/// </summary>
[JsonConverter(typeof(TemplateParameterTypeJsonConverter))]
public readonly record struct TemplateParameterType(string Value)
{
    public static readonly TemplateParameterType String = new("string");

    public static readonly TemplateParameterType Number = new("number");

    /// <summary>The wire value, for example <c>"number"</c>.</summary>
    public override string ToString() => Value ?? string.Empty;
}
