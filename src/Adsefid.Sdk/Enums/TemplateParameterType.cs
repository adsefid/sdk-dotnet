using System.Text.Json.Serialization;
using Adsefid.Sdk.Json;

namespace Adsefid.Sdk.Enums;

/// <summary>
/// The documented complete public set is { string, number }. The real server enum has been
/// observed to carry an additional, undocumented "Url" member; it is intentionally not exposed
/// here. Re-verify against the source-of-truth doc if the server starts returning it.
/// </summary>
[JsonConverter(typeof(TemplateParameterTypeJsonConverter))]
public enum TemplateParameterType
{
    String,
    Number,
}
