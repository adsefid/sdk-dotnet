namespace Adsefid.Sdk.Exceptions;

/// <summary>
/// Thrown by <see cref="Webhooks.WebhookVerifier.VerifyAndParse"/> when an incoming webhook request
/// fails verification: a malformed timestamp, a missing/invalid signature, a stale timestamp beyond
/// the allowed max age, or a payload that isn't valid JSON in the expected shape. Treat this as
/// "reject the request" — respond with an unauthorized/bad-request status rather than processing the
/// payload.
/// </summary>
public sealed class AdsefidWebhookVerificationException(string message) : AdsefidException(message);
