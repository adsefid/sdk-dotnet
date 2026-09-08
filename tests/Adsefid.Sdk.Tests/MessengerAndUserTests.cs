using System.Text;
using System.Text.Json;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Messenger.Models;
using Adsefid.Sdk.Models.Common;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class MessengerResourceTests
{
    [Fact]
    public async Task SendSingleBuildsTheRequest()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.send_single.success.json");

        await client.Messenger.SendSingleAsync(new SendSingleMessengerRequest
        {
            Message = "hi",
            Receptor = "98912xxxxxxx",
            Profile = Guid.NewGuid(),
        });

        var request = handler.Only;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/messenger/single", request.Uri.AbsolutePath);

        using var body = JsonDocument.Parse(request.Body);
        foreach (var absent in new[] { "hide", "file_id", "send_time", "local_id" })
        {
            Assert.False(body.RootElement.TryGetProperty(absent, out _), $"{absent} should be absent");
        }
    }

    [Fact]
    public async Task BulkPartialSuccessIsNotAnError()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/messenger.send_bulk.partial_success.json");

        var result = await client.Messenger.SendBulkAsync(new SendBulkMessengerRequest
        {
            Receptors = [new BulkMessengerReceptor { Receptor = "a" }, new BulkMessengerReceptor { Receptor = "b" }],
            Message = "m",
            Profile = Guid.NewGuid(),
        });

        Assert.Equal([1000, 2025], result.Receptors.Select(receptor => receptor.Status));
        Assert.Null(result.Receptors[1].MessageId);
    }

    [Fact]
    public async Task SendTemplateKeepsLeadingZeros()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.send_template.success.json");

        await client.Messenger.SendTemplateAsync(new SendTemplateMessengerRequest
        {
            TemplateId = "invoice_notice",
            Parameters = new Dictionary<string, TemplateParameterValue>
            {
                ["invoice"] = "001234",
                ["amount"] = 2,
            },
            Receptor = "98912xxxxxxx",
            Profile = Guid.NewGuid(),
        });

        using var body = JsonDocument.Parse(handler.Only.Body);
        var parameters = body.RootElement.GetProperty("parameters");
        Assert.Equal("001234", parameters.GetProperty("invoice").GetString());
        Assert.Equal(2, parameters.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task UploadFilePostsMultipartWithTheDocumentedFieldName()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.upload_file.success.json");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file-contents-here"));

        var result = await client.Messenger.UploadFileAsync(stream, "invoice.pdf", "application/pdf");

        var request = handler.Only;
        Assert.Equal("/v1/messenger/file", request.Uri.AbsolutePath);
        // The boundary is generated per request, so assert on the parts rather
        // than on the serialized bytes.
        Assert.StartsWith("multipart/form-data;", request.ContentType, StringComparison.Ordinal);
        Assert.Contains("name=file", request.Body.Replace("\"", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("invoice.pdf", request.Body, StringComparison.Ordinal);
        Assert.Contains("application/pdf", request.Body, StringComparison.Ordinal);
        Assert.Contains("file-contents-here", request.Body, StringComparison.Ordinal);

        Assert.NotEqual(Guid.Empty, result.FileId);
    }

    public static TheoryData<string, Func<AdsefidClient, Task>> InvalidCalls() => new()
    {
        { "empty message", client => client.Messenger.SendSingleAsync(new SendSingleMessengerRequest { Message = "", Receptor = "a", Profile = Guid.NewGuid() }) },
        { "empty profile", client => client.Messenger.SendSingleAsync(new SendSingleMessengerRequest { Message = "m", Receptor = "a", Profile = Guid.Empty }) },
        { "message over the limit", client => client.Messenger.SendSingleAsync(new SendSingleMessengerRequest { Message = new string('x', 4001), Receptor = "a", Profile = Guid.NewGuid() }) },
        { "no receptors", client => client.Messenger.SendBulkAsync(new SendBulkMessengerRequest { Receptors = [], Message = "m", Profile = Guid.NewGuid() }) },
        { "cancel with neither id list", client => client.Messenger.CancelAsync(new CancelMessengerRequest()) },
        { "status with neither id list", client => client.Messenger.GetStatusAsync(null, null) },
    };

    [Theory]
    [MemberData(nameof(InvalidCalls))]
    public async Task ValidationRejectsBeforeAnyRequestIsSent(string name, Func<AdsefidClient, Task> call)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.send_single.success.json");

        await Assert.ThrowsAsync<AdsefidValidationException>(() => call(client));

        Assert.True(handler.Requests.Count == 0, $"{name} must be rejected before reaching the network");
    }
}

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
