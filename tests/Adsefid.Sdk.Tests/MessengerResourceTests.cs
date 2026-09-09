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
        Assert.Equal(WebServiceMessageStatus.Scheduled, result.Receptors[0].MessageStatus);
        Assert.Equal(WebServiceResponseCode.ReceptorBlacklisted, result.Receptors[1].ErrorCode);
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
