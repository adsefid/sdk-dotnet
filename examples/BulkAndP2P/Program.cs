// Bulk and P2P SMS sends, and how to read a partial success.
//
// Both endpoints answer HTTP 200 even when some receptors failed, so a call
// that did not throw still needs its per-item results inspected. Each item.s
// Status is the raw WebServiceCode; MessageStatus and ErrorCode split it into
// the typed enum for its range, so a caller never compares ints.
using Adsefid.Sdk;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Sms.Models;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY");
var lineNumber = Environment.GetEnvironmentVariable("ADSEFID_LINE_NUMBER");
if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(lineNumber))
{
    Console.Error.WriteLine("set ADSEFID_API_KEY and ADSEFID_LINE_NUMBER");
    return 1;
}

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });

// One identical message to many receptors. LocalId is your own handle: it comes
// back here and on the status webhook, so you can match a delivery report to
// your own record without storing our message IDs.
var bulk = await client.Sms.SendBulkAsync(new SendBulkSmsRequest
{
    LineNumber = lineNumber,
    Message = "Scheduled maintenance tonight from 01:00 to 03:00.",
    Receptors =
    [
        new BulkSmsReceptor { Receptor = "09120000000", LocalId = "maint-1" },
        new BulkSmsReceptor { Receptor = "09120000001", LocalId = "maint-2" },
    ],
});

Console.WriteLine($"\nbulk group {bulk.GroupId}: {bulk.TotalCount} receptors, cost {bulk.TotalCost}");
foreach (var receptor in bulk.Receptors)
{
    Report(receptor.Receptor, receptor.LocalId, receptor.Status, receptor.MessageStatus, receptor.ErrorCode, receptor.MessageId?.ToString());
}

Console.WriteLine($"  status histogram: {string.Join(", ", bulk.Counts.Select(entry => $"{entry.Key}={entry.Value}"))}");

// A different message per receptor, in one request.
var p2p = await client.Sms.SendP2PAsync(new SendP2PSmsRequest
{
    LineNumber = lineNumber,
    Messages =
    [
        new P2PSmsMessage { Receptor = "09120000000", Message = "Hi Ali, your order #1001 shipped.", LocalId = "ship-1001" },
        new P2PSmsMessage { Receptor = "09120000001", Message = "Hi Reza, your order #1002 shipped.", LocalId = "ship-1002" },
    ],
});

Console.WriteLine($"\np2p group {p2p.GroupId}: cost {p2p.TotalCost}");
foreach (var message in p2p.Messages)
{
    Report(message.Receptor, message.LocalId, message.Status, message.MessageStatus, message.ErrorCode, message.MessageId?.ToString());
}

return 0;

// ErrorCode is set for a rejected item, MessageStatus for an accepted one; both are null
// for a code this SDK does not know yet, which is why the raw Status is still printed.
static void Report(string receptor, string? localId, int status, WebServiceMessageStatus? messageStatus, WebServiceResponseCode? errorCode, string? messageId)
{
    var label = localId ?? "-";

    if (errorCode is not null || WebServiceCode.IsErrorCode(status))
    {
        Console.WriteLine($"  {receptor,-14} ({label}) FAILED with code {status} ({errorCode?.ToString() ?? "unknown"})");
        return;
    }

    Console.WriteLine($"  {receptor,-14} ({label}) accepted as {messageId ?? "-"}: {messageStatus?.ToString() ?? status.ToString()}");
}
