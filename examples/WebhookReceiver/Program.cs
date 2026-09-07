using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Webhooks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var webhookSecret = Environment.GetEnvironmentVariable("ADSEFID_WEBHOOK_SECRET")
    ?? throw new InvalidOperationException("ADSEFID_WEBHOOK_SECRET is not set.");

app.MapPost("/webhooks/adsefid", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();

    var signature = request.Headers[WebhookHeaderNames.Signature].ToString();
    var timestamp = request.Headers[WebhookHeaderNames.Timestamp].ToString();

    WebhookEvent webhookEvent;
    try
    {
        webhookEvent = WebhookVerifier.VerifyAndParse(rawBody, signature, timestamp, webhookSecret);
    }
    catch (AdsefidWebhookVerificationException)
    {
        return Results.Unauthorized();
    }

    // Only event types this webhook endpoint is subscribed to (in your adsefid.com panel) will
    // ever arrive here — an endpoint subscribed to just "receive" never sees a StatusWebhookEvent.
    var summary = webhookEvent switch
    {
        ReceiveWebhookEvent receive =>
            $"receive: {receive.Data.Count} inbound message(s), most recent from {receive.Data[^1].Sender}",
        StatusWebhookEvent status =>
            $"status: {status.Data.Count} SMS status update(s), e.g. {status.Data[0].Id} -> {status.Data[0].StatusDelivery}",
        MessengerStatusWebhookEvent messengerStatus =>
            $"messenger.status: {messengerStatus.Data.Count} messenger status update(s), e.g. {messengerStatus.Data[0].Id} -> {messengerStatus.Data[0].StatusDelivery}",
        _ => $"unhandled event type '{webhookEvent.Type}'",
    };

    Console.WriteLine(summary);

    return Results.Ok();
});

app.Run();
