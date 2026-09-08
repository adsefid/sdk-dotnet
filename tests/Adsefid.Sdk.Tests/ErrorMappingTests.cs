using System.Net;
using System.Text.Json;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class ErrorMappingTests
{
    [Theory]
    // The envelope wins over the HTTP status: a 200 carrying an error envelope
    // is still an error.
    [InlineData("errors/error.invalid_api_key.json", HttpStatusCode.Unauthorized, false, 2018, "UNAUTHORIZED")]
    [InlineData("errors/error.rate_limit_message.json", HttpStatusCode.TooManyRequests, true, 2035, "MESSAGE_LIMIT_REACHED")]
    [InlineData("errors/error.rate_limit_request.json", HttpStatusCode.OK, true, 2036, "REQUEST_LIMIT_REACHED")]
    [InlineData("errors/error.invalid_parameter.json", HttpStatusCode.BadRequest, false, 2024, "INVALID_PARAMETER")]
    [InlineData("errors/error.unknown_code.json", HttpStatusCode.BadRequest, false, 2999, "SOME_FUTURE_SERVER_ERROR")]
    public async Task ErrorEnvelopesMapToTypedExceptions(
        string fixture,
        HttpStatusCode status,
        bool rateLimited,
        int code,
        string name)
    {
        var (client, _) = TestClient.RespondingWithFixture(fixture, status);

        var exception = await Assert.ThrowsAnyAsync<AdsefidApiException>(() => client.User.GetInfoAsync());

        Assert.Equal(code, (int)exception.Code);
        Assert.Equal(name, exception.Name);
        Assert.Equal((int)status, exception.HttpStatusCode);
        Assert.Equal(rateLimited, exception is AdsefidRateLimitException);
    }

    [Fact]
    public async Task DetailsSurviveIntact()
    {
        var (client, _) = TestClient.RespondingWithFixture("errors/error.invalid_parameter.json", HttpStatusCode.BadRequest);

        var exception = await Assert.ThrowsAsync<AdsefidApiException>(() => client.User.GetInfoAsync());

        Assert.NotNull(exception.Details);
        var details = exception.Details.Value;
        Assert.Equal(JsonValueKind.Object, details.ValueKind);
        Assert.True(details.TryGetProperty("take", out _));
        Assert.True(details.TryGetProperty("state", out _));
    }

    /// <summary>
    /// A code the enum does not know about must not break the response: the service adds codes over
    /// time, and an older SDK has to keep working against a newer service.
    /// </summary>
    [Fact]
    public async Task AnUnmappedCodeIsCarriedThroughNotRejected()
    {
        var (client, _) = TestClient.RespondingWithFixture("errors/error.unknown_code.json", HttpStatusCode.BadRequest);

        var exception = await Assert.ThrowsAsync<AdsefidApiException>(() => client.User.GetInfoAsync());

        Assert.Equal(2999, (int)exception.Code);
        Assert.False(Enum.IsDefined(exception.Code));
    }

    [Fact]
    public async Task A500WithAnHtmlBodyIsAnApiException()
    {
        var (client, _) = TestClient.RespondingWithFixture("errors/error.not_json.txt", HttpStatusCode.InternalServerError);

        var exception = await Assert.ThrowsAnyAsync<AdsefidException>(() => client.User.GetInfoAsync());

        Assert.IsNotType<AdsefidRateLimitException>(exception);
    }

    [Fact]
    public async Task ABare429IsStillARateLimit()
    {
        var (client, _) = TestClient.RespondingWithFixture("errors/error.not_json.txt", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<AdsefidRateLimitException>(() => client.User.GetInfoAsync());
    }

    [Theory]
    [InlineData("{\"status\":\"success\"}")]
    [InlineData("{\"status\":\"success\",\"data\":null}")]
    [InlineData("")]
    [InlineData("<html>nope</html>")]
    [InlineData("{\"status\":\"partial\",\"data\":{}}")]
    public async Task AMalformedSuccessEnvelopeIsAnSdkException(string body)
    {
        var (client, _) = TestClient.RespondingWith(body);

        // Whichever branch it lands in, it must be one of this SDK's own types
        // and never a raw JsonException or NullReferenceException.
        await Assert.ThrowsAnyAsync<AdsefidException>(() => client.User.GetInfoAsync());
    }

    [Fact]
    public async Task ANetworkFailureBecomesATransportException()
    {
        var inner = new HttpRequestException("connection refused");
        var (client, _) = TestClient.Build(FakeHttpMessageHandler.Throws(inner));

        var exception = await Assert.ThrowsAsync<AdsefidTransportException>(() => client.User.GetInfoAsync());

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public async Task ATimeoutBecomesATransportException()
    {
        var (client, _) = TestClient.Build(FakeHttpMessageHandler.Throws(new TaskCanceledException("timed out")));

        await Assert.ThrowsAsync<AdsefidTransportException>(() => client.User.GetInfoAsync());
    }

    /// <summary>
    /// A rate limit is an API exception, so a broad <c>catch (AdsefidApiException)</c> swallows it.
    /// Callers who want to treat rate limits specially must order their catch blocks narrowest
    /// first — this pins that it really happens.
    /// </summary>
    [Fact]
    public async Task ARateLimitIsCaughtByABroadApiExceptionHandler()
    {
        var (client, _) = TestClient.RespondingWithFixture("errors/error.rate_limit_message.json", HttpStatusCode.TooManyRequests);

        var exception = await Assert.ThrowsAnyAsync<AdsefidApiException>(() => client.User.GetInfoAsync());

        Assert.IsType<AdsefidRateLimitException>(exception);
        Assert.Equal(WebServiceResponseCode.MessageLimitReached, exception.Code);
    }
}
