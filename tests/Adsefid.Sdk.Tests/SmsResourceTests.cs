using System.Text.Json;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Models.Common;
using Adsefid.Sdk.Sms.Models;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class SmsResourceTests
{
    [Fact]
    public async Task SendSingleBuildsTheRequest()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.send_single.success.json");

        await client.Sms.SendSingleAsync(new SendSingleSmsRequest
        {
            Receptor = "98912xxxxxxx",
            LineNumber = "3000xxxx",
            Message = "hello",
        });

        var request = handler.Only;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/sms/single", request.Uri.AbsolutePath);
        Assert.Equal(TestClient.ApiKey, request.Headers["X-API-KEY"]);
        Assert.StartsWith("adsefid-dotnet/", request.Headers["User-Agent"], StringComparison.Ordinal);
        Assert.StartsWith("application/json", request.ContentType, StringComparison.Ordinal);

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("98912xxxxxxx", body.RootElement.GetProperty("receptor").GetString());
        Assert.Equal("hello", body.RootElement.GetProperty("message").GetString());
        // An omitted optional must be absent, not null.
        foreach (var absent in new[] { "send_time", "local_id", "hide", "line_selector" })
        {
            Assert.False(body.RootElement.TryGetProperty(absent, out _), $"{absent} should be absent");
        }
    }

    [Fact]
    public async Task SendSinglePassesSuppliedOptionals()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.send_single.success.json");

        await client.Sms.SendSingleAsync(new SendSingleSmsRequest
        {
            Receptor = "98912xxxxxxx",
            LineNumber = "3000xxxx",
            Message = "hello",
            LocalId = "order-1",
            Hide = true,
            LineSelector = LineSelector.BulkServiceSendBased,
            SendTime = new DateTimeOffset(2026, 4, 4, 11, 0, 0, TimeSpan.Zero),
        });

        using var body = JsonDocument.Parse(handler.Only.Body);
        Assert.Equal("order-1", body.RootElement.GetProperty("local_id").GetString());
        Assert.True(body.RootElement.GetProperty("hide").GetBoolean());
        Assert.Equal(2, body.RootElement.GetProperty("line_selector").GetInt32());
        Assert.StartsWith("2026-04-04T11:00:00", body.RootElement.GetProperty("send_time").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendSingleParsesTheResponse()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.send_single.success.json");

        var result = await client.Sms.SendSingleAsync(new SendSingleSmsRequest
        {
            Receptor = "98912xxxxxxx",
            LineNumber = "3000xxxx",
            Message = "hi",
        });

        Assert.Equal(WebServiceMessageStatus.Scheduled, result.Status);
        Assert.Equal(1, result.SegmentCount);
        Assert.Equal(120m, result.Cost);
        Assert.NotNull(result.SendTime);
    }

    /// <summary>
    /// A bulk send answers HTTP 200 even when receptors failed, so it stays a normal typed response.
    /// </summary>
    [Fact]
    public async Task BulkPartialSuccessIsNotAnError()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.send_bulk.partial_success.json");

        var result = await client.Sms.SendBulkAsync(new SendBulkSmsRequest
        {
            Receptors = [new BulkSmsReceptor { Receptor = "a" }, new BulkSmsReceptor { Receptor = "b" }],
            Message = "m",
            LineNumber = "3000xxxx",
        });

        Assert.Equal(2, result.Receptors.Count);
        Assert.Equal(1000, result.Receptors[0].Status);
        // 2025 RECEPTOR_BLACKLISTED is a response code, not a message status —
        // which is why the per-item status is a plain int.
        Assert.Equal(2025, result.Receptors[1].Status);
        Assert.Null(result.Receptors[1].MessageId);
        Assert.Equal(2, result.TotalCount);

        // The typed views split the WebServiceCode by range, so a caller never compares raw ints.
        Assert.Equal(WebServiceMessageStatus.Scheduled, result.Receptors[0].MessageStatus);
        Assert.Null(result.Receptors[0].ErrorCode);
        Assert.Null(result.Receptors[1].MessageStatus);
        Assert.Equal(WebServiceResponseCode.ReceptorBlacklisted, result.Receptors[1].ErrorCode);
    }

    [Fact]
    public async Task P2PPartialSuccessIsNotAnError()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.send_p2p.partial_success.json");

        var result = await client.Sms.SendP2PAsync(new SendP2PSmsRequest
        {
            Messages = [new P2PSmsMessage { Receptor = "a", Message = "x" }],
            LineNumber = "3000xxxx",
        });

        Assert.Equal([1000, 2014], result.Messages.Select(message => message.Status));
        Assert.Equal([WebServiceMessageStatus.Scheduled, null], result.Messages.Select(message => message.MessageStatus));
        Assert.Equal([null, WebServiceResponseCode.InvalidReceptor], result.Messages.Select(message => message.ErrorCode));
    }

    /// <summary>
    /// A number-typed parameter sent as a string keeps its exact digits: the service substitutes
    /// such a value verbatim.
    /// </summary>
    [Fact]
    public async Task SendTemplateKeepsExactNumericValues()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.send_template.success.json");

        await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
        {
            TemplateId = "otp_login",
            Parameters = new Dictionary<string, TemplateParameterValue>
            {
                ["code"] = "459122",
                ["invoice"] = "001234",
                ["exact_amount"] = "1.50",
                ["quantity"] = 2,
                ["rate"] = 19.99m,
            },
            Receptor = "98912xxxxxxx",
            LineNumber = "3000xxxx",
        });

        using var body = JsonDocument.Parse(handler.Only.Body);
        var parameters = body.RootElement.GetProperty("parameters");
        Assert.Equal("001234", parameters.GetProperty("invoice").GetString());
        Assert.Equal("1.50", parameters.GetProperty("exact_amount").GetString());
        Assert.Equal(2, parameters.GetProperty("quantity").GetInt32());
        Assert.Equal(19.99m, parameters.GetProperty("rate").GetDecimal());
    }

    [Fact]
    public async Task SendTemplateEchoesParametersBack()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.send_template.success.json");

        var result = await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
        {
            TemplateId = "otp_login",
            Parameters = new Dictionary<string, TemplateParameterValue> { ["code"] = "459122" },
            Receptor = "98912xxxxxxx",
            LineNumber = "3000xxxx",
        });

        Assert.Equal("459122", result.Parameters["code"].AsString);
        Assert.True(result.Parameters["minutes"].IsNumber);
        Assert.Equal(2m, result.Parameters["minutes"].AsNumber);
    }

    [Fact]
    public async Task GetStatusJoinsIdsIntoCsvQueryParams()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.get_status.success.json");
        var messageId = Guid.NewGuid();

        await client.Sms.GetStatusAsync([messageId], ["l1"]);

        var request = handler.Only;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/v1/sms/status", request.Uri.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(request.Uri.Query);
        Assert.Equal(messageId.ToString(), query["message_ids"]);
        Assert.Equal("l1", query["local_ids"]);
    }

    [Fact]
    public async Task GetReceivedBuildsTheQuery()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.get_received.success.json");

        var result = await client.Sms.GetReceivedAsync("3000xxxx", 10);

        var query = System.Web.HttpUtility.ParseQueryString(handler.Only.Uri.Query);
        Assert.Equal("3000xxxx", query["line_number"]);
        Assert.Equal("10", query["count"]);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public async Task CancelBuildsTheRequest()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.cancel.success.json");

        await client.Sms.CancelAsync(new CancelSmsRequest { LocalIds = ["l1"] });

        Assert.Equal("/v1/sms/cancel", handler.Only.Uri.AbsolutePath);
    }

    public static TheoryData<string, Func<AdsefidClient, Task>> InvalidCalls() => new()
    {
        { "empty receptor", client => client.Sms.SendSingleAsync(new SendSingleSmsRequest { Receptor = "", LineNumber = "3000", Message = "m" }) },
        { "empty line number", client => client.Sms.SendSingleAsync(new SendSingleSmsRequest { Receptor = "a", LineNumber = "", Message = "m" }) },
        { "empty message", client => client.Sms.SendSingleAsync(new SendSingleSmsRequest { Receptor = "a", LineNumber = "3000", Message = "" }) },
        { "message over the limit", client => client.Sms.SendSingleAsync(new SendSingleSmsRequest { Receptor = "a", LineNumber = "3000", Message = new string('x', 901) }) },
        { "invalid local id", client => client.Sms.SendSingleAsync(new SendSingleSmsRequest { Receptor = "a", LineNumber = "3000", Message = "m", LocalId = "-bad" }) },
        { "no receptors", client => client.Sms.SendBulkAsync(new SendBulkSmsRequest { Receptors = [], Message = "m", LineNumber = "3000" }) },
        { "no messages", client => client.Sms.SendP2PAsync(new SendP2PSmsRequest { Messages = [], LineNumber = "3000" }) },
        { "status with neither id list", client => client.Sms.GetStatusAsync(null, null) },
        { "cancel with neither id list", client => client.Sms.CancelAsync(new CancelSmsRequest()) },
        { "received count of zero", client => client.Sms.GetReceivedAsync("3000", 0) },
        { "received count over the limit", client => client.Sms.GetReceivedAsync("3000", 500) },
        { "received with empty line number", client => client.Sms.GetReceivedAsync("", null) },
    };

    [Theory]
    [MemberData(nameof(InvalidCalls))]
    public async Task ValidationRejectsBeforeAnyRequestIsSent(string name, Func<AdsefidClient, Task> call)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.send_single.success.json");

        await Assert.ThrowsAsync<AdsefidValidationException>(() => call(client));

        Assert.True(handler.Requests.Count == 0, $"{name} must be rejected before reaching the network");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(499)]
    public async Task ReceiveCountBoundariesAreAccepted(int count)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/sms.get_received.success.json");

        await client.Sms.GetReceivedAsync("3000xxxx", count);

        Assert.Single(handler.Requests);
    }
}
