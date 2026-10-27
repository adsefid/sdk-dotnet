namespace Adsefid.Sdk.Http;

internal static class CsvHelper
{
    /// <summary>
    /// Escapes each value and joins them with a literal comma, dropping duplicates (the service
    /// collapses them anyway). The comma is the list separator, and leaving it unescaped keeps the
    /// URL about a third shorter than <c>%2C</c> would.
    /// </summary>
    public static string? Join(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var joined = string.Join(',', values.Distinct(StringComparer.Ordinal).Select(Uri.EscapeDataString));
        return joined.Length == 0 ? null : joined;
    }

    public static string BuildIdsQuery(IReadOnlyCollection<Guid>? messageIds, IReadOnlyCollection<string>? localIds)
    {
        var parts = new List<string>(2);
        var messageIdsCsv = Join(messageIds?.Distinct().Select(static id => id.ToString()));
        var localIdsCsv = Join(localIds);

        if (messageIdsCsv is not null)
        {
            parts.Add($"message_ids={messageIdsCsv}");
        }

        if (localIdsCsv is not null)
        {
            parts.Add($"local_ids={localIdsCsv}");
        }

        return parts.Count == 0 ? string.Empty : $"?{string.Join('&', parts)}";
    }
}
