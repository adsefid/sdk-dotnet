using Adsefid.Sdk.Http;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class CsvHelperTests
{
    [Fact]
    public void JoinReturnsNullForNothingToJoin()
    {
        Assert.Null(CsvHelper.Join((IReadOnlyCollection<string>?)null));
        Assert.Null(CsvHelper.Join(Array.Empty<string>()));
        Assert.Null(CsvHelper.Join((IReadOnlyCollection<Guid>?)null));
        Assert.Null(CsvHelper.Join(Array.Empty<Guid>()));
    }

    [Fact]
    public void JoinCommaSeparatesValues()
    {
        Assert.Equal("a", CsvHelper.Join(new[] { "a" }));
        Assert.Equal("a,b,c", CsvHelper.Join(new[] { "a", "b", "c" }));
    }

    [Fact]
    public void BuildIdsQueryOmitsTheQueryStringEntirelyWhenBothAreEmpty()
    {
        Assert.Equal(string.Empty, CsvHelper.BuildIdsQuery(null, null));
        Assert.Equal(string.Empty, CsvHelper.BuildIdsQuery(Array.Empty<Guid>(), Array.Empty<string>()));
    }

    [Fact]
    public void BuildIdsQueryIncludesOnlyThePartsThatHaveValues()
    {
        var id = Guid.NewGuid();

        Assert.Equal($"?message_ids={id}", CsvHelper.BuildIdsQuery([id], null));
        Assert.Equal("?local_ids=l1", CsvHelper.BuildIdsQuery(null, ["l1"]));
        Assert.Equal($"?message_ids={id}&local_ids=l1", CsvHelper.BuildIdsQuery([id], ["l1"]));
    }

    [Fact]
    public void BuildIdsQueryEscapesValues()
    {
        var query = CsvHelper.BuildIdsQuery(null, ["a b", "c&d"]);

        Assert.DoesNotContain(" ", query, StringComparison.Ordinal);
        var parsed = System.Web.HttpUtility.ParseQueryString(query.TrimStart('?'));
        Assert.Equal("a b,c&d", parsed["local_ids"]);
    }
}
