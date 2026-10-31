using System.Text.Json;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Messenger.Models;
using Adsefid.Sdk.Sms.Models;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class CancellationTests
{
    [Theory]
    [InlineData(0, 0, 1, true)]
    [InlineData(2000, 0, 1, false)]
    [InlineData(2001, 0, 1, true)]
    [InlineData(0, 2000, 1, false)]
    [InlineData(0, 2001, 1, true)]
    [InlineData(1000, 1000, 1, false)]
    [InlineData(1000, 1001, 1, true)]
    [InlineData(1000, 1000, 3, false)]
    public async Task CancellationCountsDistinctIdsAndPreservesBody(int messageCount, int localCount, int repetitions, bool rejected)
    {
        var messageIds = Enumerable.Range(0, messageCount)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
            .SelectMany(id => Enumerable.Repeat(id, repetitions)).ToArray();
        var localIds = Enumerable.Range(0, localCount)
            .Select(i => $"00000000-0000-0000-0000-{i:D12}")
            .SelectMany(id => Enumerable.Repeat(id, repetitions)).ToArray();

        foreach (var area in new[] { "sms", "messenger" })
        {
            var (client, handler) = TestClient.RespondingWithFixture($"envelopes/{area}.cancel.success.json");
            Func<Task> cancel = area == "sms"
                ? () => client.Sms.CancelAsync(new CancelSmsRequest { MessageIds = messageIds, LocalIds = localIds })
                : () => client.Messenger.CancelAsync(new CancelMessengerRequest { MessageIds = messageIds, LocalIds = localIds });

            if (rejected)
            {
                await Assert.ThrowsAsync<AdsefidValidationException>(cancel);
                Assert.Empty(handler.Requests);
            }
            else
            {
                await cancel();
                Assert.Equal(HttpMethod.Post, handler.Only.Method);
                Assert.Equal($"/v1/{area}/cancel", handler.Only.Uri.AbsolutePath);
                using var body = JsonDocument.Parse(handler.Only.Body);
                Assert.Equal(messageIds, body.RootElement.GetProperty("message_ids").EnumerateArray().Select(id => id.GetGuid()));
                Assert.Equal(localIds, body.RootElement.GetProperty("local_ids").EnumerateArray().Select(id => id.GetString()));
            }
        }
    }
    [Theory]
    [InlineData("sms")]
    [InlineData("messenger")]
    public async Task CancellationPreservesNullableIdsAndItemErrors(string area)
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/cancel.partial_success.json");
        if (area == "sms")
        {
            var result = await client.Sms.CancelAsync(new CancelSmsRequest { LocalIds = ["missing-local"] });
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceMessageStatus.Canceled, result.CancelledMessages[0].MessageStatus);
            Assert.Equal(new[] { 1001, 2046, 2029, 2999 }, result.FailedToCancel.Select(item => item.Status));
            Assert.Null(result.FailedToCancel[1].LocalId);
            Assert.Null(result.FailedToCancel[2].MessageId);
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceResponseCode.InvalidMessageIds, result.FailedToCancel[1].ErrorCode);
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceResponseCode.InvalidLocalIds, result.FailedToCancel[2].ErrorCode);
            Assert.Null(result.FailedToCancel[3].ErrorCode);
        }
        else
        {
            var result = await client.Messenger.CancelAsync(new CancelMessengerRequest { LocalIds = ["missing-local"] });
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceMessageStatus.Canceled, result.CancelledMessages[0].MessageStatus);
            Assert.Equal(new[] { 1001, 2046, 2029, 2999 }, result.FailedToCancel.Select(item => item.Status));
            Assert.Null(result.FailedToCancel[1].LocalId);
            Assert.Null(result.FailedToCancel[2].MessageId);
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceResponseCode.InvalidMessageIds, result.FailedToCancel[1].ErrorCode);
            Assert.Equal(Adsefid.Sdk.Enums.WebServiceResponseCode.InvalidLocalIds, result.FailedToCancel[2].ErrorCode);
            Assert.Null(result.FailedToCancel[3].ErrorCode);
        }
    }
}
