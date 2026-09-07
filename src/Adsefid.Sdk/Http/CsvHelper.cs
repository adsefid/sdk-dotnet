namespace Adsefid.Sdk.Http;

internal static class CsvHelper
{
    public static string? Join(IReadOnlyCollection<string>? values) =>
        values is null || values.Count == 0 ? null : string.Join(',', values);

    public static string? Join(IReadOnlyCollection<Guid>? values) =>
        values is null || values.Count == 0 ? null : string.Join(',', values.Select(static v => v.ToString()));

    public static string BuildIdsQuery(IReadOnlyCollection<Guid>? messageIds, IReadOnlyCollection<string>? localIds)
    {
        var parts = new List<string>();
        var messageIdsCsv = Join(messageIds);
        var localIdsCsv = Join(localIds);

        if (messageIdsCsv is not null)
        {
            parts.Add($"message_ids={Uri.EscapeDataString(messageIdsCsv)}");
        }

        if (localIdsCsv is not null)
        {
            parts.Add($"local_ids={Uri.EscapeDataString(localIdsCsv)}");
        }

        return parts.Count == 0 ? string.Empty : $"?{string.Join('&', parts)}";
    }
}
