namespace Adsefid.Sdk.Webhooks;

/// <summary>Fired for <see cref="WebhookEventTypes.MessengerStatus"/>: one or more Messenger messages' delivery status changed.</summary>
public sealed record MessengerStatusWebhookEvent : WebhookEvent
{
    /// <summary>The status updates carried by this delivery.</summary>
    public required IReadOnlyList<StatusUpdateItem> Data { get; init; }
}
