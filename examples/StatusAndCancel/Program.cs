// Delivery status, cancelling scheduled messages, and reading inbound SMS.
//
// Status and cancel both accept message IDs (ours) and local IDs (yours) in one
// call; their combined distinct count may not exceed 2000, which the SDK checks
// before making the request.
using Adsefid.Sdk;
using Adsefid.Sdk.Sms.Models;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY");
var lineNumber = Environment.GetEnvironmentVariable("ADSEFID_LINE_NUMBER");
if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(lineNumber))
{
    Console.Error.WriteLine("set ADSEFID_API_KEY and ADSEFID_LINE_NUMBER");
    return 1;
}

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });

// Schedule far enough ahead that there is something to cancel.
var sendTime = DateTimeOffset.UtcNow.AddHours(2);
var sent = await client.Sms.SendSingleAsync(new SendSingleSmsRequest
{
    Receptor = "09120000000",
    LineNumber = lineNumber,
    Message = "This one is scheduled, and about to be cancelled.",
    SendTime = sendTime,
    LocalId = "demo-cancel-1",
});
Console.WriteLine($"scheduled {sent.MessageId} for {sendTime:O}");

// Look it up by our ID and by your own LocalId at the same time.
var status = await client.Sms.GetStatusAsync([sent.MessageId], ["demo-cancel-1"]);
Console.WriteLine($"\nstatus for {status.Receptors.Count} message(s):");
foreach (var receptor in status.Receptors)
{
    var delivered = receptor.DeliveryTime?.ToString("O") ?? "not yet";
    Console.WriteLine($"  {receptor.MessageId} -> {receptor.Status} (delivered: {delivered})");
}

// Cancelling reports each message separately: one already sent cannot be
// recalled and comes back under FailedToCancel.
var cancelled = await client.Sms.CancelAsync(new CancelSmsRequest { MessageIds = [sent.MessageId] });
Console.WriteLine($"\ncancelled {cancelled.CancelledMessages.Count}, failed to cancel {cancelled.FailedToCancel.Count}");
foreach (var item in cancelled.FailedToCancel)
{
    Console.WriteLine($"  {item.MessageId} could not be cancelled: {item.Status}");
}

// Inbound messages. count must be 1..499; since filters by arrival time.
var received = await client.Sms.GetReceivedAsync(lineNumber, 50, DateTimeOffset.UtcNow.AddDays(-1));
Console.WriteLine($"\n{received.Messages.Count} inbound message(s) in the last 24h:");
foreach (var message in received.Messages)
{
    Console.WriteLine($"  from {message.Sender} at {message.ReceiveDate:O}: {message.Message}");
}

return 0;
