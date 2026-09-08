using System.Net;
using System.Text;

namespace Adsefid.Sdk.Tests.Infrastructure;

/// <summary>One request as the fake handler saw it.</summary>
internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    string? ContentType);

/// <summary>
/// An <see cref="HttpMessageHandler"/> that records every request and replays a fixed response.
/// Injected through <see cref="AdsefidClientOptions.HttpClient"/>, which is the SDK's test seam.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    private readonly List<RecordedRequest> _requests = [];

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    public IReadOnlyList<RecordedRequest> Requests => _requests;

    public RecordedRequest Only =>
        _requests.Count == 1
            ? _requests[0]
            : throw new InvalidOperationException($"Expected exactly 1 request, got {_requests.Count}.");

    public static FakeHttpMessageHandler Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    public static FakeHttpMessageHandler Throws(Exception exception) =>
        new(_ => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // A real handler chain observes the token before doing any work.
        cancellationToken.ThrowIfCancellationRequested();

        // Buffer the body now: RequestExecutor disposes the request message as
        // soon as the call unwinds, so reading it lazily would come back empty.
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase),
            body,
            request.Content?.Headers.ContentType?.ToString()));

        return _responder(request);
    }
}
