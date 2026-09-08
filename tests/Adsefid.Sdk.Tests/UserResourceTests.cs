using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class UserResourceTests
{
    [Theory]
    [InlineData("envelopes/user.get_info.success.json", "/v1/user/info")]
    [InlineData("envelopes/user.get_lines.success.json", "/v1/user/lines")]
    [InlineData("envelopes/user.get_profiles.success.json", "/v1/user/profiles")]
    public async Task AccountEndpointsHitTheRightPath(string fixture, string path)
    {
        var (client, handler) = TestClient.RespondingWithFixture(fixture);

        _ = path switch
        {
            "/v1/user/info" => (object)await client.User.GetInfoAsync(),
            "/v1/user/lines" => await client.User.GetLinesAsync(),
            _ => await client.User.GetProfilesAsync(),
        };

        Assert.Equal(HttpMethod.Get, handler.Only.Method);
        Assert.Equal(path, handler.Only.Uri.AbsolutePath);
    }

    [Fact]
    public async Task GetTemplatesParsesTheDocumentedShape()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_templates.success.json");

        var result = await client.User.GetTemplatesAsync(TemplateState.Approved, 0, 50);

        var query = System.Web.HttpUtility.ParseQueryString(handler.Only.Uri.Query);
        Assert.Equal("approved", query["state"]);
        Assert.Equal("0", query["skip"]);
        Assert.Equal("50", query["take"]);

        Assert.Equal(1, result.Total);
        var item = Assert.Single(result.Items);
        Assert.Equal(TemplateState.Approved, item.State);
        Assert.Equal(TemplateParameterType.String, item.Parameters["OTPCode"]);
        Assert.Equal(TemplateParameterType.Number, item.Parameters["amount"]);
        Assert.Null(item.Description);
    }

    /// <summary>
    /// The live service emits a parameter type this SDK does not model. Dropping the entry keeps
    /// the typed dictionary honest rather than surfacing a value callers cannot switch on.
    /// </summary>
    [Fact]
    public async Task GetTemplatesDropsUndocumentedParameterTypes()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/user.get_templates.unknown_type.json");

        var result = await client.User.GetTemplatesAsync();

        var parameters = result.Items[0].Parameters;
        Assert.False(parameters.ContainsKey("link"));
        Assert.Equal(2, parameters.Count);
    }

    [Fact]
    public async Task GetTemplatesSendsNoQueryStringWhenNothingWasSupplied()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_templates.success.json");

        await client.User.GetTemplatesAsync();

        Assert.Equal(string.Empty, handler.Only.Uri.Query);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task TakeBoundariesAreAccepted(int take)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_templates.success.json");

        await client.User.GetTemplatesAsync(take: take);

        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 101)]
    [InlineData(-1, null)]
    public async Task OutOfRangePagingIsRejectedBeforeSending(int? skip, int? take)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/user.get_templates.success.json");

        await Assert.ThrowsAsync<AdsefidValidationException>(
            () => client.User.GetTemplatesAsync(skip: skip, take: take));

        Assert.Empty(handler.Requests);
    }
}
