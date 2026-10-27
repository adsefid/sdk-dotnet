# AGENTS.md — Adsefid.Sdk

## Scope

This repository contains the .NET client SDK for the adsefid.com SMS Web Service API
(`Adsefid.Sdk`) and its optional Microsoft dependency injection integration
(`Adsefid.Sdk.DependencyInjection`). Each package has its own project and version.
Equivalent SDKs exist for the same API in sibling repositories (`sdk-js`, `sdk-php`, `sdk-python`,
`sdk-go`); a behavior change here should generally be considered for parity there.

## Source of truth

The API surface (endpoints, field names, types, validation rules, enums, example payloads, webhook
behavior) is defined by the published adsefid.com SMS Web Service API documentation.

**Pin: this SDK is built against doc version `v1.14.0`.** Re-read the relevant section of that
documentation before changing any endpoint, request/response model, or enum. If the doc has moved on
since `v1.14.0`, diff it against what's implemented here before trusting either side.

Each package follows independent Semantic Versioning from its project `<Version>`; never copy the
API-document version into package metadata. Record package and API-document versions in the README.

A small number of facts below are empirically observed behaviors of the live API that are easy to
get wrong from a literal reading of the documentation's prose or pseudo-code. Trust these notes over
an ambiguous doc reading:

- Webhook signatures are plain Base64, not hex-then-Base64. The signature is HMAC-SHA256 over the
  literal string `"{timestamp}.{raw_body}"`, and the raw digest bytes are Base64-encoded directly —
  there is no intermediate hex-encoding step, even though a literal reading of some spec pseudo-code
  can suggest one. See `Webhooks/WebhookVerifier.cs`.
- `TemplateState` and `TemplateParameterType` are extensible `readonly record struct` values: named
  members for the documented values only, and any other wire value is preserved in `.Value`, never
  dropped or thrown on. Add a named member only for a value the documentation lists. See
  `Enums/TemplateParameterType.cs`.
- `error.details` uses the shared typed `ApiErrorDetails` shape: optional `Errors` maps field names
  (or rejected cancel IDs) to `{Code,Name}`, and optional `Items` carries indexed bulk/P2P errors.
  Keep numeric codes forward-compatible.
- `line_selector` is required when the account has two or more lines with the same number. Without
  it the service answers `LINE_NOT_FOUND` (2002), even for a number the account owns.
- GET requests are capped at 8,000 characters of path and query. The service's proxy answers an HTML
  HTTP 414 above 8 KiB, so the documented 2,000-ID status lookup cannot be sent as one GET (about 200
  IDs fit). The SDK joins IDs with literal commas, drops duplicates, and refuses an oversized GET
  locally with its validation error. Cancel is a POST and is not affected.
- Timestamps carry up to 7 fractional digits (`2026-09-19T11:43:10.4030292+03:30`) and mixed offsets;
  parsers must accept both.
- Bulk/P2P item validation happens in the API. Validate request-level fields locally, but send item
  values unchanged so valid siblings can still succeed.

## Architecture map

| Folder | Responsibility |
|---|---|
| `Http/` | Transport (`RequestExecutor`), envelope parsing, error-to-exception mapping, multipart upload, CSV/query helpers, client-side validation helpers |
| `src/Adsefid.Sdk.DependencyInjection/` | Optional `IServiceCollection.AddAdsefid` typed-client registration |
| `Json/` | The single source-generated `AdsefidJsonContext` (`JsonSerializerContext`) plus the handful of custom `JsonConverter<T>` types it references (`TemplateState`, `TemplateParameterType`, `TemplateParameterValue`) |
| `Exceptions/` | The typed exception hierarchy every failure surfaces through |
| `Enums/` | `LineSelector`, `WebServiceMessageStatus`, `WebServiceResponseCode`, `TemplateState`, `TemplateParameterType`, plus the `WebServiceCode` range helpers behind the per-item `MessageStatus`/`ErrorCode` views on bulk/P2P results |
| `Models/Common/` | Shapes shared across resources: `ResponseEnvelope<T>`, `ApiErrorPayload`/`ErrorEnvelope`, typed `ApiErrorDetails`/field/item errors, `TemplateParameterValue` |
| `Sms/`, `Messenger/`, `User/` | One resource client per API area (`SmsResource`, `MessengerResource`, `UserResource`) plus their `Models/` request/response types |
| `Webhooks/` | `WebhookVerifier` (signature + timestamp verification, payload dispatch), the `WebhookEvent` record hierarchy, and `WebhookHeaderNames`/`WebhookEventTypes` constants (use these instead of typing header/type strings) |

## Adding a new endpoint

1. Add request/response types under `<Area>/Models/` (e.g. `Sms/Models/`, `Messenger/Models/`), one
   file per operation, matching the existing naming pattern. Every JSON property gets an explicit
   `[JsonPropertyName("snake_case_name")]` attribute — never rely on a naming policy.
2. Register both the request type and `ResponseEnvelope<TResponse>` (or `ResponseEnvelope<List<T>>`
   for a bare-array endpoint) with `[JsonSerializable(...)]` on `Json/AdsefidJsonContext.cs`.
3. Add the method to the relevant resource class (`SmsResource`, `MessengerResource`, or
   `UserResource`): validate the doc-stated simple client-side constraints first (required fields,
   max lengths, count limits — see `Http/Validation.cs`), then call `_executor.GetAsync` /
   `PostJsonAsync` / `PostMultipartAsync`.
4. Update the resource-reference table in `README.md`.

## Hard rules

- **Every change ships with tests.** `tests/Adsefid.Sdk.Tests` is xUnit (`make test`). Transport
  behaviour is driven through the public `AdsefidClient` with a fake `HttpMessageHandler` supplied
  via `AdsefidClientOptions.HttpClient`; the internal pure helpers (`Validation`, `CsvHelper`,
  `Limits`) are reached through the `InternalsVisibleTo` in `Directory.Build.props`. Do not test
  `RequestExecutor` or the internal resource constructors directly — go through the public surface,
  which is what a consumer sees.
- **Golden fixtures are shared across all five SDKs.** `tests/Adsefid.Sdk.Tests/fixtures` is
  byte-identical to the same tree in the sibling repositories. Never edit one in isolation: change
  it in all five and regenerate every `CHECKSUMS.txt`, or `FixturesIntegrityTests` fails.
- **`.editorconfig` requires `utf-8-bom`.** A new `.cs` file without a BOM fails `make lint`.
- **No magic limits.** Every request bound lives in `Http/Limits.cs` and is referenced by name.
- **Template parameter values.** `TemplateParameterValue` stores numbers as `decimal`, so an exact
  decimal round-trips. A `number` parameter may legitimately travel as a JSON *string* — that is how
  leading zeros (`"001234"`) and exact decimals (`"1.50"`) reach the service intact, since it
  substitutes a numeric string verbatim. Its `default` is a distinct unassigned state that refuses
  to serialize; do not "simplify" that away.
- **The webhook secret is Base64.** A webhook endpoint's secret is 32 random bytes shown
  Base64-encoded in the panel, and the service signs with the **decoded** bytes.
  `WebhookVerifier.VerifyAndParse` decodes before keying the HMAC, and has a `ReadOnlySpan<byte>`
  overload for a pre-decoded key. The primary overloads take the raw body as `ReadOnlySpan<byte>`;
  the `string` overloads re-encode as UTF-8 and exist for convenience only. Keying the HMAC with the UTF-8 bytes of the Base64 string does not
  verify against the live service.
- **An empty webhook key is rejected** (an empty HMAC key signs anything), and verification compares
  decoded digest bytes without allocating. `ResponseEnvelope<T>`, `ErrorEnvelope` and `ApiErrorPayload`
  are internal wire shapes, not public API.
- **Uploads never dispose the caller's stream** (it is wrapped in `NonDisposingStream`), validate the
  media type, and cap seekable content at `Limits.MessengerFileMaxBytes`. The default `HttpClient` is
  built on a `SocketsHttpHandler` with a bounded connection lifetime.
- **Request bodies omit nulls.** `AdsefidJsonContext` sets
  `DefaultIgnoreCondition = WhenWritingNull`, so an unset optional is absent from the body rather
  than an explicit `null`, matching the sibling SDKs.
- **Keep dependency injection optional.** Core `Adsefid.Sdk` stays BCL-only. The companion package
  alone references `Microsoft.Extensions.Http`, registers the concrete client as transient, and
  returns `IHttpClientBuilder` for caller-owned transport configuration. Do not add a one-member
  client interface.

- No reflection-based JSON, ever. Every serializable type must be registered on
  `AdsefidJsonContext`; if `dotnet build` doesn't fail but a type silently falls back to reflection,
  that's a bug — check the `[JsonSerializable]` list first.
- No magic string/int literals for domain values — add a named member to the relevant enum in
  `Enums/`, or a named constant, instead of inlining `2035` or `"pendingapproval"`.
- Public API surface gets XML doc comments (`/// <summary>`) with real content — behavior, units,
  formats, and which exceptions a member throws and when — not comments that just restate the member
  name. Internal/private implementation gets no comments except where a genuinely non-obvious
  constraint requires one (e.g. the webhook signature's "no hex intermediate step" note). No narration comments anywhere.
- Throw on error, always. Never introduce a `Result`/`Either`-style return wrapper — resource methods
  return the strongly-typed success payload directly and throw a typed `AdsefidException` subclass
  otherwise. Bulk/P2P partial-success items are data, not exceptions — see their own per-item
  status/code fields.
- No retry logic anywhere in this SDK, by design.

## Build

```bash
dotnet build
dotnet pack src/Adsefid.Sdk/Adsefid.Sdk.csproj -c Release
dotnet pack src/Adsefid.Sdk.DependencyInjection/Adsefid.Sdk.DependencyInjection.csproj -c Release
```

MSBuild settings live in this repository's own `Directory.Build.props`.
