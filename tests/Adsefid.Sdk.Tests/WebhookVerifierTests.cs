using System.Security.Cryptography;
using System.Text;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Tests.Infrastructure;
using Adsefid.Sdk.Webhooks;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class WebhookVerifierTests
{
    private static readonly TimeSpan ACentury = TimeSpan.FromDays(365 * 100);

    private static (string Secret, string Timestamp, string Signature, string Tampered, byte[] Body) Vector()
    {
        var vector = Fixtures.Json("webhooks/signature_vector.json");
        return (
            vector.GetProperty("secret").GetString()!,
            vector.GetProperty("timestamp").GetString()!,
            vector.GetProperty("signature").GetString()!,
            vector.GetProperty("tampered_signature").GetString()!,
            Fixtures.Bytes(vector.GetProperty("body_file").GetString()!));
    }

    private static string BodyText() => Encoding.UTF8.GetString(Vector().Body);

    /// <summary>
    /// The cross-SDK vector. It proves the HMAC key is the Base64-DECODED secret bytes rather than
    /// the UTF-8 bytes of the Base64 string the panel shows. Its timestamp is fixed, so the
    /// staleness window has to be opened wide.
    /// </summary>
    [Fact]
    public void TheGoldenVectorVerifies()
    {
        var vector = Vector();

        var result = WebhookVerifier.VerifyAndParse(BodyText(), vector.Signature, vector.Timestamp, vector.Secret, ACentury);

        var receive = Assert.IsType<ReceiveWebhookEvent>(result);
        Assert.Equal(WebhookEventTypes.Receive, receive.Type);
        Assert.Equal(1, receive.Attempt);
        Assert.Equal("1", receive.Version);
        Assert.Single(receive.Data);
        Assert.NotEmpty(receive.Data[0].Sender);
    }

    /// <summary>
    /// Regression guard for the key-derivation fix: keying the HMAC with the text of the Base64
    /// secret is what this SDK used to do, and it never matched a real delivery.
    /// </summary>
    [Fact]
    public void ASignatureKeyedWithTheBase64TextIsRejected()
    {
        var vector = Vector();
        var input = Encoding.UTF8.GetBytes($"{vector.Timestamp}.{BodyText()}");
        var wrong = "v1=" + Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(vector.Secret), input));

        Assert.NotEqual(vector.Signature, wrong);
        Assert.Throws<AdsefidWebhookVerificationException>(
            () => WebhookVerifier.VerifyAndParse(BodyText(), wrong, vector.Timestamp, vector.Secret, ACentury));
    }

    /// <summary>
    /// The byte overload is the primary one: it signs the exact bytes off the wire, with no
    /// string round trip in between.
    /// </summary>
    [Fact]
    public void TheRawBodyBytesOverloadVerifiesTheGoldenVector()
    {
        var vector = Vector();

        var result = WebhookVerifier.VerifyAndParse(vector.Body, vector.Signature, vector.Timestamp, vector.Secret, ACentury);

        Assert.IsType<ReceiveWebhookEvent>(result);
    }

    [Fact]
    public void TheRawBodyBytesAndDecodedKeyOverloadVerifiesTheGoldenVector()
    {
        var vector = Vector();
        ReadOnlySpan<byte> key = Convert.FromBase64String(vector.Secret);

        var result = WebhookVerifier.VerifyAndParse(vector.Body, vector.Signature, vector.Timestamp, key, ACentury);

        Assert.IsType<ReceiveWebhookEvent>(result);
    }

    [Fact]
    public void TheDecodedKeyOverloadAcceptsRawBytes()
    {
        var vector = Vector();
        var key = Convert.FromBase64String(vector.Secret);

        var result = WebhookVerifier.VerifyAndParse(
            BodyText(), vector.Signature, vector.Timestamp, key.AsSpan(), ACentury);

        Assert.IsType<ReceiveWebhookEvent>(result);
    }

    [Fact]
    public void ASecretThatIsNotBase64IsRejected()
    {
        var vector = Vector();

        var exception = Assert.Throws<AdsefidWebhookVerificationException>(
            () => WebhookVerifier.VerifyAndParse(BodyText(), vector.Signature, vector.Timestamp, "not base64 !!", ACentury));

        Assert.Contains("Base64", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("webhooks/receive.body.json", typeof(ReceiveWebhookEvent), WebhookEventTypes.Receive)]
    [InlineData("webhooks/status.body.json", typeof(StatusWebhookEvent), WebhookEventTypes.Status)]
    [InlineData("webhooks/messenger_status.body.json", typeof(MessengerStatusWebhookEvent), WebhookEventTypes.MessengerStatus)]
    public void EachEventTypeParsesToItsOwnClass(string fixture, Type expectedType, string expectedEvent)
    {
        var secret = Vector().Secret;
        var body = Fixtures.Bytes(fixture);
        var timestamp = WebhookSigner.Now();

        var result = WebhookVerifier.VerifyAndParse(
            Encoding.UTF8.GetString(body),
            WebhookSigner.Sign(secret, timestamp, body),
            timestamp,
            secret);

        Assert.IsType(expectedType, result);
        Assert.Equal(expectedEvent, result.Type);
    }

    [Fact]
    public void StatusItemsCarryATypedDeliveryStatus()
    {
        var secret = Vector().Secret;
        var body = Fixtures.Bytes("webhooks/status.body.json");
        var timestamp = WebhookSigner.Now();

        var result = WebhookVerifier.VerifyAndParse(
            Encoding.UTF8.GetString(body), WebhookSigner.Sign(secret, timestamp, body), timestamp, secret);

        var status = Assert.IsType<StatusWebhookEvent>(result);
        Assert.Equal(WebServiceMessageStatus.Delivered, status.Data[0].StatusDelivery);
        Assert.Equal("order-10001", status.Data[0].LocalId);
    }

    public static TheoryData<string, Func<Exception?>> Rejections()
    {
        var vector = Vector();
        var bodyText = Encoding.UTF8.GetString(vector.Body);
        var timestamp = WebhookSigner.Now();
        var signature = WebhookSigner.Sign(vector.Secret, timestamp, vector.Body);

        var stale = WebhookSigner.Now(-301);
        var future = WebhookSigner.Now(301);

        return new TheoryData<string, Func<Exception?>>
        {
            { "tampered signature", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, vector.Tampered, vector.Timestamp, vector.Secret, ACentury)) },
            { "tampered body", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText + " ", signature, timestamp, vector.Secret)) },
            { "missing v1= prefix", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, signature[3..], timestamp, vector.Secret)) },
            { "signature is not base64", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, "v1=not-base-64-!!", timestamp, vector.Secret)) },
            { "non-numeric timestamp", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, signature, "not-a-number", vector.Secret)) },
            { "empty timestamp", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, signature, string.Empty, vector.Secret)) },
            { "stale timestamp", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, WebhookSigner.Sign(vector.Secret, stale, vector.Body), stale, vector.Secret)) },
            { "future timestamp", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, WebhookSigner.Sign(vector.Secret, future, vector.Body), future, vector.Secret)) },
            { "wrong secret", () => Record.Exception(() => WebhookVerifier.VerifyAndParse(bodyText, signature, timestamp, Convert.ToBase64String(Encoding.UTF8.GetBytes("a-different-32-byte-secret-value")))) },
        };
    }

    /// <summary>
    /// Asserts the exception type only, never the message: a doubly-invalid request may report
    /// either failure depending on the order the checks run in.
    /// </summary>
    [Theory]
    [MemberData(nameof(Rejections))]
    public void RejectsInvalidRequests(string name, Func<Exception?> act)
    {
        Assert.True(act() is AdsefidWebhookVerificationException, $"{name} should have been rejected");
    }

    [Fact]
    public void MaxAgeWidensTheWindow()
    {
        var vector = Vector();
        var timestamp = WebhookSigner.Now(-600);
        var signature = WebhookSigner.Sign(vector.Secret, timestamp, vector.Body);
        var bodyText = BodyText();

        Assert.Throws<AdsefidWebhookVerificationException>(
            () => WebhookVerifier.VerifyAndParse(bodyText, signature, timestamp, vector.Secret));

        var result = WebhookVerifier.VerifyAndParse(
            bodyText, signature, timestamp, vector.Secret, TimeSpan.FromMinutes(15));

        Assert.IsType<ReceiveWebhookEvent>(result);
    }

    /// <summary>All five SDKs report the signature first for a doubly-invalid request.</summary>
    [Fact]
    public void TheSignatureIsCheckedBeforeFreshness()
    {
        var vector = Vector();

        var exception = Assert.Throws<AdsefidWebhookVerificationException>(
            () => WebhookVerifier.VerifyAndParse(BodyText(), vector.Tampered, WebhookSigner.Now(-600), vector.Secret));

        Assert.Contains("signature", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("webhooks/unknown_type.body.json")]
    public void AnUnknownEventTypeIsRejected(string fixture)
    {
        var secret = Vector().Secret;
        var body = Fixtures.Bytes(fixture);
        var timestamp = WebhookSigner.Now();

        Assert.Throws<AdsefidWebhookVerificationException>(() => WebhookVerifier.VerifyAndParse(
            Encoding.UTF8.GetString(body), WebhookSigner.Sign(secret, timestamp, body), timestamp, secret));
    }

    [Theory]
    [InlineData("<html>nope</html>")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"type\":\"receive\"}")]
    public void AnUnparseablePayloadIsRejected(string rawBody)
    {
        var secret = Vector().Secret;
        var body = Encoding.UTF8.GetBytes(rawBody);
        var timestamp = WebhookSigner.Now();

        Assert.Throws<AdsefidWebhookVerificationException>(() => WebhookVerifier.VerifyAndParse(
            rawBody, WebhookSigner.Sign(secret, timestamp, body), timestamp, secret));
    }

    [Fact]
    public void HeaderConstantsAreTheWireProtocol()
    {
        // Renaming any of these breaks every deployed receiver.
        Assert.Equal("X-Atlas-Webhook-Id", WebhookHeaderNames.Id);
        Assert.Equal("X-Atlas-Webhook-Signature", WebhookHeaderNames.Signature);
        Assert.Equal("X-Atlas-Webhook-Timestamp", WebhookHeaderNames.Timestamp);
        Assert.Equal("X-Atlas-Webhook-Event", WebhookHeaderNames.Event);
        Assert.Equal("X-Atlas-Webhook-Attempt", WebhookHeaderNames.Attempt);
    }

    [Fact]
    public void EventTypeConstants()
    {
        Assert.Equal("receive", WebhookEventTypes.Receive);
        Assert.Equal("status", WebhookEventTypes.Status);
        Assert.Equal("messenger.status", WebhookEventTypes.MessengerStatus);
    }
}
