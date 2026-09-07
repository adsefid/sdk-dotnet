# Adsefid.Sdk

[![CI](https://github.com/adsefid/sdk-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/adsefid/sdk-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Adsefid.Sdk.svg)](https://www.nuget.org/packages/Adsefid.Sdk)

A .NET client SDK for the [adsefid.com SMS Web Service](https://adsefid.com) REST API: SMS, Messenger and User
endpoints, plus outgoing webhook signature verification.

- Single target framework: `net8.0`
- Zero external dependencies (BCL only), zero reflection — JSON is handled by a source-generated
  `System.Text.Json` serializer context
- Throws typed exceptions on error; never returns a `Result`/`Either` wrapper

## Requirements

- .NET 8.0 SDK or later

## Install

```bash
dotnet add package Adsefid.Sdk
```

## Quickstart

```csharp
using Adsefid.Sdk;
using Adsefid.Sdk.Sms.Models;

var client = new AdsefidClient(new AdsefidClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY")!,
});

var result = await client.Sms.SendSingleAsync(new SendSingleSmsRequest
{
    Receptor = "98912****567",
    LineNumber = "3000xxxx",
    Message = "Hello from Adsefid.Sdk",
    LocalId = "order-10001",
});

Console.WriteLine($"message_id={result.MessageId} status={result.Status} cost={result.Cost}");
```

A runnable version of this is in [`examples/Quickstart`](examples/Quickstart).

## Auth setup

The SDK never reads environment variables itself — read your API key explicitly and pass it in:

```csharp
var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY")
    ?? throw new InvalidOperationException("ADSEFID_API_KEY is not set.");

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });
```

## Configuration

```csharp
var client = new AdsefidClient(new AdsefidClientOptions
{
    ApiKey = apiKey,

    // Override the base URL (defaults to "https://api.adsefid.com")
    BaseUrl = "https://api.adsefid.com",

    // Supply your own HttpClient, e.g. one created via IHttpClientFactory.
    // When omitted, the SDK lazily creates and caches a default HttpClient
    // per (BaseUrl, ApiKey) pair, with BaseAddress and the X-API-KEY header
    // already configured.
    HttpClient = httpClientFactory.CreateClient("adsefid"),
});
```

To control timeouts, configure them on the `HttpClient` you supply (`HttpClient.Timeout`, or via
`IHttpClientFactory` handler configuration) — the SDK does not impose its own timeout.

## Resource reference

| Resource | SDK method | HTTP endpoint | Notes |
|---|---|---|---|
| Sms | SendSingleAsync(request) | POST /v1/sms/single | |
| Sms | SendBulkAsync(request) | POST /v1/sms/bulk | Partial success is a normal typed return, not an exception |
| Sms | SendP2PAsync(request) | POST /v1/sms/p2p | Partial success is a normal typed return, not an exception |
| Sms | SendTemplateAsync(request) | POST /v1/sms/template | |
| Sms | GetStatusAsync(messageIds?, localIds?) | GET /v1/sms/status | |
| Sms | CancelAsync(request) | POST /v1/sms/cancel | |
| Sms | GetReceivedAsync(lineNumber, count?, since?) | GET /v1/sms/receive | |
| Messenger | SendSingleAsync(request) | POST /v1/messenger/single | |
| Messenger | SendBulkAsync(request) | POST /v1/messenger/bulk | Partial success is a normal typed return, not an exception |
| Messenger | SendP2PAsync(request) | POST /v1/messenger/p2p | Partial success is a normal typed return, not an exception |
| Messenger | UploadFileAsync(fileStream, fileName, contentType) | POST /v1/messenger/file | Accepts a Stream (caller owns its lifetime) |
| Messenger | CancelAsync(request) | POST /v1/messenger/cancel | |
| Messenger | SendTemplateAsync(request) | POST /v1/messenger/template | |
| Messenger | GetStatusAsync(messageIds?, localIds?) | GET /v1/messenger/status | |
| User | GetInfoAsync() | GET /v1/user/info | |
| User | GetLinesAsync() | GET /v1/user/lines | |
| User | GetProfilesAsync() | GET /v1/user/profiles | |
| User | GetTemplatesAsync(state?, skip?, take?) | GET /v1/user/templates | |

## Error handling

Every resource method throws on failure instead of returning a result wrapper:

```csharp
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Exceptions;

try
{
    var result = await client.Sms.SendSingleAsync(request);
}
catch (AdsefidRateLimitException ex)
{
    // ex.Code is MessageLimitReached (2035) or RequestLimitReached (2036)
    Console.WriteLine($"Rate limited: {ex.Name}");
}
catch (AdsefidApiException ex)
{
    Console.WriteLine($"API error {ex.Code} ({ex.Name}), HTTP {ex.HttpStatusCode}");
    if (ex.Details is { } details)
    {
        Console.WriteLine(details.GetRawText());
    }
}
catch (AdsefidValidationException ex)
{
    // Thrown client-side, before any network call (e.g. an invalid local_id format)
    Console.WriteLine($"Invalid request: {ex.Message}");
}
catch (AdsefidTransportException ex)
{
    // Network, DNS, or timeout failure
    Console.WriteLine($"Transport error: {ex.Message}");
}
```

`AdsefidApiException.Details` is a loosely-typed `JsonElement?` because its shape is endpoint-specific
(a validation map keyed by snake_case field path, a bulk item list, a cancel-specific map, or absent).

Bulk and P2P send endpoints have partial-success semantics: an HTTP 200 with `status: "success"` can
still contain some failed items. These are **not** exceptions — they come back as a normal typed
response (e.g. `SendBulkSmsResponse.Receptors`) where each item carries its own status/code.

## Rate limits

Codes `2035` (`MessageLimitReached`) and `2036` (`RequestLimitReached`) — and a bare HTTP `429` with an
unparseable body — surface as `AdsefidRateLimitException` (which derives from `AdsefidApiException`).

## Enums

| Enum | Namespace |
|---|---|
| `LineSelector` | `Adsefid.Sdk.Enums` |
| `WebServiceMessageStatus` | `Adsefid.Sdk.Enums` |
| `WebServiceResponseCode` | `Adsefid.Sdk.Enums` |
| `TemplateState` | `Adsefid.Sdk.Enums` |
| `TemplateParameterType` | `Adsefid.Sdk.Enums` |

`AdsefidApiException.Code` is parsed permissively: an unrecognized numeric code becomes
`(WebServiceResponseCode)rawInt` instead of throwing, so the SDK degrades gracefully as the API adds
new codes over time.

## File upload example

```csharp
using var fileStream = File.OpenRead("brochure.pdf");
var upload = await client.Messenger.UploadFileAsync(fileStream, "brochure.pdf", "application/pdf");

await client.Messenger.SendSingleAsync(new SendSingleMessengerRequest
{
    Message = "See attached brochure",
    Receptor = "98912****567",
    Profile = profileId,
    FileId = upload.FileId,
});
```

## Webhook verification

```csharp
using Adsefid.Sdk.Webhooks;

var app = WebApplication.Create();

app.MapPost("/webhooks/adsefid", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    var signature = request.Headers[WebhookHeaderNames.Signature].ToString();
    var timestamp = request.Headers[WebhookHeaderNames.Timestamp].ToString();

    WebhookEvent webhookEvent;
    try
    {
        webhookEvent = WebhookVerifier.VerifyAndParse(
            rawBody,
            signature,
            timestamp,
            secret: Environment.GetEnvironmentVariable("ADSEFID_WEBHOOK_SECRET")!);
    }
    catch (AdsefidWebhookVerificationException)
    {
        return Results.Unauthorized();
    }

    switch (webhookEvent)
    {
        case ReceiveWebhookEvent receive:
            foreach (var item in receive.Data)
            {
                Console.WriteLine($"Inbound SMS from {item.Sender}: {item.Message}");
            }
            break;

        case StatusWebhookEvent status:
            foreach (var item in status.Data)
            {
                Console.WriteLine($"SMS {item.Id} -> {item.StatusDelivery}");
            }
            break;

        case MessengerStatusWebhookEvent messengerStatus:
            foreach (var item in messengerStatus.Data)
            {
                Console.WriteLine($"Messenger message {item.Id} -> {item.StatusDelivery}");
            }
            break;
    }

    return Results.Ok();
});

app.Run();
```

`VerifyAndParse` throws `AdsefidWebhookVerificationException` for a bad signature, a stale timestamp
(default max age: 5 minutes, override via the `maxAge` parameter), or a malformed payload. Signature
verification uses `CryptographicOperations.FixedTimeEquals` for constant-time comparison.

`WebhookHeaderNames` (`Id`, `Signature`, `Timestamp`, `Event`, `Attempt`) exposes every header name
as a constant, so you never have to type `"X-Atlas-Webhook-Signature"` yourself.
`WebhookEventTypes` (`Receive`, `Status`, `MessengerStatus`) does the same for the `type` values.

You configure, per webhook endpoint, which event types it receives (in your adsefid.com panel) —
an endpoint subscribed only to `receive` will never see a `StatusWebhookEvent` arrive, so don't
assume every deployment gets all three; handle whichever ones you've subscribed to.

A runnable minimal API version of this is in [`examples/WebhookReceiver`](examples/WebhookReceiver).

## Versioning

This SDK follows Semantic Versioning independently of the API documentation.

- SDK version: **`0.1.0`** (`<Version>` in `Adsefid.Sdk.csproj`)
- Verified API documentation: **`v1.11.0`**

SDK releases use `v<SDK_VERSION>` tags. The two version numbers move independently.

## License

Proprietary — All rights reserved.
