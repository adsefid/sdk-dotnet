# Adsefid.Sdk

[![CI](https://github.com/adsefid/sdk-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/adsefid/sdk-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Adsefid.Sdk.svg)](https://www.nuget.org/packages/Adsefid.Sdk)

A .NET client for the [adsefid.com](https://adsefid.com) SMS Web Service API. It sends SMS and
Messenger messages (single, bulk, P2P and template), checks delivery status, cancels unsent
messages, reads inbound SMS, looks up account data, and verifies signed webhooks.

The core package targets `net8.0`, has no dependencies beyond the BCL, and uses a source-generated
`System.Text.Json` context, so it works with trimming and Native AOT. It is verified against API
documentation **v1.15.0**. Sibling SDKs exist for Go, JavaScript, PHP and Python with the same
shape.

## Install

```bash
dotnet add package Adsefid.Sdk
dotnet add package Adsefid.Sdk.DependencyInjection   # optional, for IHttpClientFactory
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
    Receptor = "989120000000",
    LineNumber = "983000",
    LineSelector = LineSelector.BulkServiceSendBased,
    Message = "Hello from Adsefid.Sdk",
    LocalId = "order-10001",
});

Console.WriteLine($"message_id={result.MessageId} status={result.Status} cost={result.Cost}");
```

The SDK never reads environment variables. Pass the API key yourself. It goes out as the
`X-API-KEY` header on every request. Create one `AdsefidClient` and reuse it; it is safe to share
across threads.

**Line selector.** An account can own several lines with the same number, each with its own
selector (see `client.User.GetLinesAsync()`). When it does, `LineSelector` is required on every SMS
send; without it the service answers `InvalidLineSelector`.

## Configuration

```csharp
var client = new AdsefidClient(new AdsefidClientOptions
{
    ApiKey = apiKey,
    BaseUrl = "https://api.adsefid.com",              // default
    UserAgent = "my-service/1.0.0",                   // default: adsefid-dotnet/<SDK_VERSION>
    HttpClient = httpClientFactory.CreateClient("adsefid"), // optional
});
```

Without a custom `HttpClient`, all clients share one static instance whose pooled connections
are recycled every two minutes, so DNS changes are picked up. The SDK sets the absolute request
URI and the API key header itself, so your client needs no `BaseAddress`. The SDK imposes no
timeout of its own: set `HttpClient.Timeout`, or pass a `CancellationToken` to any call.

### Dependency injection

```csharp
builder.Services
    .AddAdsefid(_ => new AdsefidClientOptions
    {
        ApiKey = builder.Configuration["Adsefid:ApiKey"]
            ?? throw new InvalidOperationException("Adsefid:ApiKey is not set."),
    })
    .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(30));
```

`AddAdsefid` returns an `IHttpClientBuilder`, so handlers, proxies, logging and timeouts go
there. Do not set `HttpClient` in the options factory. Typed clients are transient, so inject
`AdsefidClient` into transient or scoped services and do not capture it in a singleton.

## Methods

Every method is async, takes an optional `CancellationToken`, and returns the typed response.

| Resource | Methods | Endpoint |
|---|---|---|
| `client.Sms` | `SendSingleAsync`, `SendBulkAsync`, `SendP2PAsync`, `SendTemplateAsync` | `POST /v1/sms/{single,bulk,p2p,template}` |
| `client.Sms` | `GetStatusAsync(messageIds?, localIds?)`, `CancelAsync`, `GetReceivedAsync(lineNumber, count?, since?)` | `QUERY /v1/sms/status`, `POST /v1/sms/cancel`, `GET /v1/sms/receive` |
| `client.Messenger` | `SendSingleAsync`, `SendBulkAsync`, `SendP2PAsync`, `SendTemplateAsync` | `POST /v1/messenger/{single,bulk,p2p,template}` |
| `client.Messenger` | `UploadFileAsync(stream, fileName, contentType)` | `POST /v1/messenger/file` |
| `client.Messenger` | `GetStatusAsync(messageIds?, localIds?)`, `CancelAsync` | `QUERY /v1/messenger/status`, `POST /v1/messenger/cancel` |
| `client.User` | `GetInfoAsync`, `GetLinesAsync`, `GetProfilesAsync`, `GetTemplatesAsync(state?, skip?, take?)` | `GET /v1/user/{info,lines,profiles,templates}` |

Cancel accepts messages that are awaiting approval, queued or scheduled, with no refund, and at
most 2,000 distinct IDs across both lists. Status lookups use `QUERY` with JSON arrays, supporting up to 2,000 distinct IDs in one request.
Unmatched cancellation IDs appear in `failed_to_cancel` with error codes 2046 or 2029; a missing
local ID match has a null message ID. Check each item's status or error code.

## Bulk and P2P: partial success is not an error

A bulk or P2P call returns HTTP 200 even when some items are rejected. Each item's `Status` is
either a message status (1000 to 1999) or an error code (2000 and up). `MessageStatus` and
`ErrorCode` give the typed enum for the matching range, and are `null` for the other range or for
a code this SDK does not know yet:

```csharp
foreach (var item in result.Receptors)
{
    Console.WriteLine(item.ErrorCode is { } error
        ? $"{item.Receptor}: rejected ({error})"
        : $"{item.Receptor}: {item.MessageStatus?.ToString() ?? item.Status.ToString()}");
}
```

Item values reach the API unchanged, so valid items still go out. If every item is rejected, the
call throws `AdsefidApiException` with `Details.Items` listing each rejected index. Only
request-level fields are checked locally.

## Template parameters

A parameter the template declares as `number` can be sent as a number or a string. The service
substitutes a numeric string as written, so a string is the only way to keep exact digits:

```csharp
await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
{
    TemplateId = "invoice_notice",
    Parameters = new Dictionary<string, TemplateParameterValue>
    {
        ["invoice"] = "001234", // renders 001234, not 1234
        ["amount"]  = "1.50",   // renders 1.50, not 1.5
        ["count"]   = 2,
        ["rate"]    = 19.99m,   // decimal round-trips exactly
    },
    Receptor = "09120000000",
    LineNumber = "983000",
    LineSelector = LineSelector.BulkServiceSendBased,
});
```

`TemplateParameterValue` converts implicitly from `string`, `int`, `long`, `decimal` and
`double`. Read a value back with `AsString` or `AsNumber`. A `default` value is unassigned and
throws when serialized. Money fields (`Cost`, `TotalCost`, `CreditLeft`) are `decimal`.

## File upload

```csharp
using var fileStream = File.OpenRead("brochure.pdf");
var upload = await client.Messenger.UploadFileAsync(fileStream, "brochure.pdf", "application/pdf");

await client.Messenger.SendSingleAsync(new SendSingleMessengerRequest
{
    Message = "See attached brochure",
    Receptor = "989120000000",
    Profile = profileId,
    FileId = upload.FileId,
});
```

File content is limited to 15 MiB inclusive (`MessengerResource.MaxFileBytes`). A seekable
stream above that limit throws `AdsefidValidationException` before any request is sent; the
service answers `FileTooLarge` (2047) for anything else that is too large and enforces a MIME
allowlist. The SDK reads your stream but never closes it. `contentType` must be a valid media type.

## Errors

Every failure is a typed exception. All derive from `AdsefidException`.

| Exception | Meaning |
|---|---|
| `AdsefidValidationException` | A local check failed. No request was sent. |
| `AdsefidApiException` | The API returned an error envelope, or a non-2xx status without one. Has `Code`, `Name`, `HttpStatusCode` and optional `Details`. |
| `AdsefidRateLimitException` | Derives from `AdsefidApiException`. Codes 2035 and 2036, or any HTTP 429. |
| `AdsefidTransportException` | Network failure, timeout, or a response body that could not be parsed. |
| `AdsefidWebhookVerificationException` | A webhook failed verification. |

```csharp
try
{
    var result = await client.Sms.SendSingleAsync(request);
}
catch (AdsefidRateLimitException ex)    // catch before the base type
{
    Console.WriteLine($"Rate limited: {ex.Name}");
}
catch (AdsefidApiException ex)
{
    Console.WriteLine($"API error {ex.Code} ({ex.Name}), HTTP {ex.HttpStatusCode}");
    if (ex.Details?.Errors is { } errors)
    {
        foreach (var (field, error) in errors)
        {
            Console.WriteLine($"{field}: {error.Name} ({(int)error.Code})");
        }
    }
}
catch (AdsefidValidationException ex)
{
    Console.WriteLine($"Invalid request: {ex.Message}");
}
catch (AdsefidTransportException ex)
{
    Console.WriteLine($"Transport error: {ex.Message}");
}
```

A non-2xx response with no error envelope has code `-1` and name `UNKNOWN_ERROR`. A bare 429 has
code 2036 and name `RATE_LIMITED`. `Details.Errors` maps field names to
`{ Code, Name }`, and `Details.Items` carries indexed bulk/P2P errors. The SDK sends each request
once and never retries. Add backoff in your own `HttpClient` handler if you need it.

## Enums and unknown values

`LineSelector`, `WebServiceMessageStatus` and `WebServiceResponseCode` are enums in
`Adsefid.Sdk.Enums`. `TemplateState` and `TemplateParameterType` are value types with named
members (`TemplateState.Approved`, `TemplateParameterType.Number`). A value the service adds
later is kept as its raw number or string, for example `new TemplateState("archived")`, and never
fails parsing.

## Webhooks

```csharp
app.MapPost("/webhooks/adsefid", async (HttpRequest request) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer); // verify the exact bytes, before any JSON parsing
    var rawBody = buffer.ToArray();

    WebhookEvent webhookEvent;
    try
    {
        webhookEvent = WebhookVerifier.VerifyAndParse(
            rawBody,
            request.Headers[WebhookHeaderNames.Signature].ToString(),
            request.Headers[WebhookHeaderNames.Timestamp].ToString(),
            secret: Environment.GetEnvironmentVariable("ADSEFID_WEBHOOK_SECRET")!);
    }
    catch (AdsefidWebhookVerificationException)
    {
        return Results.Unauthorized(); // do not say why
    }

    switch (webhookEvent)
    {
        case ReceiveWebhookEvent receive:
            foreach (var item in receive.Data) Console.WriteLine($"SMS from {item.Sender}: {item.Message}");
            break;
        case StatusWebhookEvent status:
            foreach (var item in status.Data) Console.WriteLine($"SMS {item.Id} -> {item.StatusDelivery}");
            break;
        case MessengerStatusWebhookEvent messenger:
            foreach (var item in messenger.Data) Console.WriteLine($"Messenger {item.Id} -> {item.StatusDelivery}");
            break;
    }

    return Results.Ok();
});
```

The panel shows the signing secret Base64-encoded, and the service signs with the decoded bytes.
Pass the secret exactly as shown; `VerifyAndParse` decodes it. If you store the decoded key, use
the `ReadOnlySpan<byte>` overload. An empty secret or key is always rejected. A stale timestamp
(default five minutes, change it with `maxAge`) or a bad signature throws
`AdsefidWebhookVerificationException`.

`WebhookHeaderNames` and `WebhookEventTypes` hold the exact wire names. Each endpoint only
receives the event types you subscribed it to in the panel. Deliveries can repeat, so make your
handler idempotent and return a 2xx quickly.

## Using this SDK with coding agents

[AGENTS.md](AGENTS.md) holds the repository rules. These points prevent most wrong guesses:

- Pass the API key and webhook secret explicitly. The SDK reads no environment variables.
- Treat a bulk or P2P response as data, not an error. Check each item's status.
- Send exact numeric text (`"001234"`, `"1.50"`) as a string `TemplateParameterValue`.
- Give `VerifyAndParse` the raw request bytes, never re-serialized JSON.
- There are no retries and no automatic batching. Split large requests yourself.
- Catch `AdsefidRateLimitException` before `AdsefidApiException`, and branch on `Code`.

## Examples and contributing

Each directory under `examples/` is a console project excluded from the NuGet package: `Account`,
`Quickstart`, `BulkAndP2P`, `Templates`, `StatusAndCancel`, `Messenger` and `WebhookReceiver`.
`Account` sends nothing, so try it first.

```bash
export ADSEFID_API_KEY=...
export ADSEFID_LINE_NUMBER=983000
dotnet run --project examples/Quickstart
```

To work on the SDK: `make lint`, `make build` and `make test`. See [AGENTS.md](AGENTS.md).

## Versioning and license

Semantic Versioning, independent of the API documentation. Core package **1.0.0**, dependency
injection package **1.0.0**. Core releases use `v<version>` tags and the DI package uses
`dependency-injection-v<version>`. See [CHANGELOG.md](CHANGELOG.md). MIT licensed, see
[LICENSE](LICENSE).
