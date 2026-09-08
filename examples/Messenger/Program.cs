// The Messenger resource end to end: upload an attachment, send it, check status.
//
// Messenger sends go through a "profile" configured in your adsefid.com panel
// rather than an SMS line, and allow a longer message body (4000 characters
// against SMS's 900). File upload is the only multipart endpoint in the API;
// the SDK reads the stream you pass but never disposes it.
using System.Text;
using Adsefid.Sdk;
using Adsefid.Sdk.Messenger.Models;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("set ADSEFID_API_KEY");
    return 1;
}

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });

var profiles = await client.User.GetProfilesAsync();
if (profiles.Count == 0)
{
    Console.Error.WriteLine("no messenger profiles on this account — add one in the adsefid.com panel first");
    return 1;
}

Console.WriteLine($"{profiles.Count} messenger profile(s):");
foreach (var profile in profiles)
{
    Console.WriteLine($"  {profile.Id} {profile.Name} ({profile.Messenger})");
}

// A FileStream works the same way; the SDK streams whatever you hand it.
var attachmentPath = Environment.GetEnvironmentVariable("ADSEFID_ATTACHMENT");
await using Stream attachment = attachmentPath is null
    ? new MemoryStream(Encoding.UTF8.GetBytes("Statement for September 2026\nTotal: 1,250,000 IRR\n"))
    : File.OpenRead(attachmentPath);

var uploaded = await client.Messenger.UploadFileAsync(
    attachment,
    Path.GetFileName(attachmentPath) ?? "statement.txt",
    "text/plain");
Console.WriteLine($"\nuploaded attachment as file_id {uploaded.FileId}");

var sent = await client.Messenger.SendSingleAsync(new SendSingleMessengerRequest
{
    Message = "Your statement is attached.",
    Receptor = "09120000000",
    Profile = profiles[0].Id,
    FileId = uploaded.FileId,
    LocalId = "statement-2026-09",
});
Console.WriteLine($"\nsent {sent.MessageId} via {sent.Messenger}: {sent.Status} (cost {sent.Cost})");

var status = await client.Messenger.GetStatusAsync([sent.MessageId], null);
foreach (var receptor in status.Receptors)
{
    Console.WriteLine($"  {receptor.MessageId} -> {receptor.Status}");
}

return 0;
