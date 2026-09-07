namespace Adsefid.Sdk.Webhooks;

/// <summary>The <c>type</c> values a webhook payload can carry, matching <see cref="WebhookEvent.Type"/>.</summary>
public static class WebhookEventTypes
{
    /// <summary>Inbound SMS was received. Parsed as <see cref="ReceiveWebhookEvent"/>.</summary>
    public const string Receive = "receive";

    /// <summary>An SMS's delivery status changed. Parsed as <see cref="StatusWebhookEvent"/>.</summary>
    public const string Status = "status";

    /// <summary>A Messenger message's delivery status changed. Parsed as <see cref="MessengerStatusWebhookEvent"/>.</summary>
    public const string MessengerStatus = "messenger.status";
}
