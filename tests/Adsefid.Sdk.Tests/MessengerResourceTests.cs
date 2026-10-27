using System.Text;
using System.Text.Json;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Messenger;
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
    public async Task BulkItemErrorsReachTheApiForPartialAcceptance()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.send_bulk.partial_success.json");

        await client.Messenger.SendBulkAsync(new SendBulkMessengerRequest
        {
            Receptors =
            [
                new BulkMessengerReceptor { Receptor = "98912xxxxxxx" },
                new BulkMessengerReceptor { Receptor = "", LocalId = "-invalid" },
            ],
            Message = "m",
            Profile = Guid.NewGuid(),
        });

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task P2PItemErrorsReachTheApiForPartialAcceptance()
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.send_p2p.partial_success.json");

        await client.Messenger.SendP2PAsync(new SendP2PMessengerRequest
        {
            Receptors =
            [
                new P2PMessengerReceptor { Receptor = "98912xxxxxxx", Message = "ok" },
                new P2PMessengerReceptor { Receptor = "", Message = "", LocalId = "-invalid" },
            ],
            Profile = Guid.NewGuid(),
        });

        Assert.Single(handler.Requests);
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

    [Fact]
    public async Task UploadFileLeavesTheCallersStreamOpen()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/messenger.upload_file.success.json");
        using var stream = new MemoryStream([1, 2, 3]);

        await client.Messenger.UploadFileAsync(stream, "a.png", "image/png");

        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData("not a media type")]
    [InlineData("image/png; x=")]
    public async Task UploadFileRejectsAnInvalidContentType(string contentType)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.upload_file.success.json");
        using var stream = new MemoryStream([1]);

        await Assert.ThrowsAsync<AdsefidValidationException>(() => client.Messenger.UploadFileAsync(stream, "a.png", contentType));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(MessengerResource.MaxFileBytes, true)]
    [InlineData(MessengerResource.MaxFileBytes + 1, false)]
    public async Task UploadFileEnforcesTheFileSizeLimit(long length, bool accepted)
    {
        var (client, handler) = TestClient.RespondingWithFixture("envelopes/messenger.upload_file.success.json");
        using var stream = new SparseStream(length);

        if (accepted)
        {
            await client.Messenger.UploadFileAsync(stream, "a.mp4", "video/mp4");
            return;
        }

        await Assert.ThrowsAsync<AdsefidValidationException>(() => client.Messenger.UploadFileAsync(stream, "a.mp4", "video/mp4"));
        Assert.Empty(handler.Requests);
    }

    private sealed class SparseStream(long length) : Stream
    {
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, length - _position);
            Array.Clear(buffer, offset, read);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
