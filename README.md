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

    // Defaults to "adsefid-dotnet/<SDK_VERSION>".
    UserAgent = "my-service/1.0.0",

    // Supply your own HttpClient, e.g. one created via IHttpClientFactory.
    // When omitted, the SDK lazily creates and caches a default HttpClient
    // per (BaseUrl, ApiKey) pair, with BaseAddress and the X-API-KEY header
    // already configured.
    HttpClient = httpClientFactory.CreateClient("adsefid"),
});
```

To control timeouts, configure them on the `HttpClient` you supply (`HttpClient.Timeout`, or via
`IHttpClientFactory` handler configuration) — the SDK does not impose its own timeout.

Monetary response properties (`Cost`, `TotalCost`, and `CreditLeft`) use `decimal` and may contain fractional values.

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

`AdsefidApiException.Details` is a loosely-typed `JsonElement?` because its shape is endpoint-specific.

`details` is not one shape — the service picks one per endpoint:

| When | Shape | Example |
|---|---|---|
| Request validation (`2024 INVALID_PARAMETER`) | `{"errors": {field: message}}` — snake_case field paths, **string** values | `{"errors":{"take":"invalid value for take"}}` |
| Single send | `{field: message}` — flat, no wrapper | `{"receptor":"invalid value for receptor"}` |
| Bulk / P2P | `{"errors": {...}, "messages": [{"index": n, "errors": {...}}]}` — `index` is the position in *your* array, so gaps are normal | `{"errors":{},"messages":[{"index":2,"errors":{"local_id":"invalid value for local_id"}}]}` |
| Cancel | `{field: [value, ...]}` — the one shape whose values are **arrays** | `{"local_ids":["order-10001"]}` |
| Anything else | absent or `null` | |

Decode it defensively for the endpoint you called rather than assuming a single shape.

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
new codes over time. `TemplateParameterType` covers the documented set only — the live service also
emits an undocumented third value, and such a parameter is dropped from `UserTemplate.Parameters`
rather than surfaced as an enum member that does not exist.

## Template parameters, leading zeros and decimals

`TemplateParameterValue` holds either a string or a number, with implicit conversions from
`string`, `int`, `long`, `decimal`, and `double`. A parameter the template declares as `number` may
be sent **either** as a JSON number or as a JSON string, and the service substitutes a numeric
string verbatim — so a string is the only way to keep a value's exact digits:

```csharp
await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
{
    TemplateId = "invoice_notice",
    Parameters = new Dictionary<string, TemplateParameterValue>
    {
        ["invoice"] = "001234", // renders as 001234 — the number 1234 would lose the zeros
        ["amount"]  = "1.50",   // renders as 1.50   — the number 1.5 would lose the zero
        ["count"]   = 2,        // an ordinary integer
        ["rate"]    = 19.99m,   // a decimal, which round-trips exactly
    },
    Receptor = "09120000000",
    LineNumber = "3000xxxx",
});
```

Numbers are held as `decimal`, so an ordinary decimal survives a round trip that `double` would
have perturbed; reading one back gives `value.AsNumber`, and a string gives `value.AsString`. Reach
for a string whenever the rendered text must match the digits you supplied — invoice and account
numbers, zero-padded codes, and money amounts with a fixed number of decimal places.

A `default(TemplateParameterValue)` was never assigned and is not a usable value: serializing one
throws rather than silently emitting JSON `null`.

See [`examples/Templates`](examples/Templates) for a runnable version.

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

### The signing secret is Base64

Your endpoint's signing secret is shown in the adsefid.com panel as the Base64 encoding of 32
random bytes, and the service signs with **those raw bytes** — not with the text of the Base64
string. Pass the secret exactly as the panel shows it and `WebhookVerifier.VerifyAndParse` decodes
it for you; a secret that is not valid Base64 throws `AdsefidWebhookVerificationException`. If you
already hold the decoded key, use the overload that takes a `ReadOnlySpan<byte>`.

## Development

```bash
make deps    # dotnet restore
make fmt     # dotnet format
make lint    # dotnet format --verify-no-changes --severity warn
make build   # dotnet build -c Release
make test    # dotnet test -c Release --no-build
```

The suite is xUnit, under `tests/Adsefid.Sdk.Tests`. It drives the public `AdsefidClient` through a
fake `HttpMessageHandler` supplied via `AdsefidClientOptions.HttpClient`, and reaches the internal
pure helpers (`Validation`, `CsvHelper`, `Limits`) through `InternalsVisibleTo`. Golden fixtures
under `tests/Adsefid.Sdk.Tests/fixtures` are byte-identical to the same tree in the sibling SDK
repositories, and `FixturesIntegrityTests` verifies them against `CHECKSUMS.txt`.

### Examples

Every directory under `examples/` is a runnable console project, excluded from the NuGet package
via `<IsPackable>false</IsPackable>`:

```bash
export ADSEFID_API_KEY=...
export ADSEFID_LINE_NUMBER=3000xxxx

dotnet run --project examples/Account          # account info, lines, profiles, templates; client config
dotnet run --project examples/Quickstart       # send one SMS, with full error triage
dotnet run --project examples/BulkAndP2P       # bulk + P2P sends, and reading a partial success
dotnet run --project examples/Templates        # list templates and send one, incl. exact numeric values
dotnet run --project examples/StatusAndCancel  # delivery status, cancelling, inbound messages
dotnet run --project examples/Messenger        # upload an attachment and send it via a messenger profile
dotnet run --project examples/WebhookReceiver  # verify and dispatch inbound webhooks
```

`examples/Account` sends nothing, so it is the safest one to try first.

## Versioning

This SDK follows Semantic Versioning independently of the API documentation.

- SDK version: **`0.3.0`** (`<Version>` in `Adsefid.Sdk.csproj`)
- Verified API documentation: **`v1.11.0`**

SDK releases use `v<SDK_VERSION>` tags. The two version numbers move independently.

## License

MIT — see [LICENSE](LICENSE).
