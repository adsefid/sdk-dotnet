using System.Text;
using System.Text.Json;

namespace Adsefid.Sdk.Tests.Infrastructure;

/// <summary>
/// Access to the golden fixtures, which are byte-identical copies of the same tree in the sibling
/// SDK repositories.
/// </summary>
internal static class Fixtures
{
    public static string Directory { get; } =
        Path.Combine(AppContext.BaseDirectory, "fixtures");

    /// <summary>
    /// Reads one fixture as raw bytes. Webhook verification signs the exact bytes on the wire, so
    /// anything feeding a signature must come from here rather than being re-serialized.
    /// </summary>
    public static byte[] Bytes(string name) =>
        File.ReadAllBytes(Path.Combine(Directory, name.Replace('/', Path.DirectorySeparatorChar)));

    public static string Text(string name) => Encoding.UTF8.GetString(Bytes(name));

    public static JsonElement Json(string name) =>
        JsonDocument.Parse(Bytes(name)).RootElement.Clone();

    public static IReadOnlyList<(string Sha256, string Name)> Manifest() =>
        Text("CHECKSUMS.txt")
            .Trim()
            .Split('\n')
            .Select(line => line.Split("  ", 2))
            .Select(parts => (parts[0], parts[1]))
            .ToList();
}
