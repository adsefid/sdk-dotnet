// Bulk and P2P SMS sends, and how to read a partial success.
//
// Both endpoints answer HTTP 200 even when some receptors failed, so a call
// that did not throw still needs its per-item results inspected. Each item's
// Status is a plain int: below 2000 it is a delivery status, 2000 and above it
// is an error code explaining why that one receptor was rejected.
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
    Report(receptor.Receptor, receptor.LocalId, receptor.Status, receptor.MessageId?.ToString());
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
    Report(message.Receptor, message.LocalId, message.Status, message.MessageId?.ToString());
}

return 0;

// A status below 2000 is a WebServiceMessageStatus; 2000 and above is a
// WebServiceResponseCode for that single receptor.
static void Report(string receptor, string? localId, int status, string? messageId)
{
    var label = localId ?? "-";

    if (status >= 2000)
    {
        Console.WriteLine($"  {receptor,-14} ({label}) FAILED with code {status} ({(WebServiceResponseCode)status})");
        return;
    }

    Console.WriteLine($"  {receptor,-14} ({label}) accepted as {messageId ?? "-"}: {(WebServiceMessageStatus)status}");
}
