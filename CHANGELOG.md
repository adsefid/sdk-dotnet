# Changelog

## Unreleased

### Breaking
- `TemplateState` and `TemplateParameterType` are now `readonly record struct` values with static
  members instead of enums. An unknown wire value is preserved in `.Value` instead of throwing, and an
  unrecognized template parameter type is no longer dropped from `UserTemplate.Parameters`.
- `ResponseEnvelope<T>`, `ErrorEnvelope` and `ApiErrorPayload` are now internal.
- `EnablePackageValidation` is on for both packages.

### Fixed
- `AdsefidClient` keeps a path in `BaseUrl` (for example `https://host/webservice/api`); it was
  dropped before, so every call returned 404.
- Status lookups send ids joined by literal commas, and a GET whose path and query pass 8,000
  characters throws `AdsefidValidationException` instead of reaching the gateway's HTML 414.
- `WebhookVerifier` rejects an empty signing key and any malformed signature as
  `AdsefidWebhookVerificationException`.
- `UploadFileAsync` no longer disposes the caller's stream, and an invalid `contentType` throws
  `AdsefidValidationException` instead of `FormatException`.

### Added
- `MessengerResource.MaxFileBytes`; seekable uploads above 15 MiB are rejected before sending.
- Request and response bodies are handled as UTF-8 bytes; webhook verification does not allocate.
- The shared default `HttpClient` recycles pooled connections every two minutes.
- `make test` builds first, so it never runs stale binaries.
