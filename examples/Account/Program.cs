// Account endpoints, plus configuring the client beyond the defaults.
//
// Nothing here sends a message, so it is the safest example to run first
// against a real API key.
using Adsefid.Sdk;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("set ADSEFID_API_KEY");
    return 1;
}

// The SDK never retries a request. Anything beyond one attempt — retries, a
// proxy, connection pooling, tracing — belongs in the HttpClient you supply.
// The SDK does not dispose a client you pass in; its lifetime is yours.
using var httpClient = new HttpClient(new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
})
{
    Timeout = TimeSpan.FromSeconds(20),
};

var client = new AdsefidClient(new AdsefidClientOptions
{
    ApiKey = apiKey,
    // BaseUrl is normally only needed to point at a mock server.
    BaseUrl = Environment.GetEnvironmentVariable("ADSEFID_BASE_URL") ?? "https://api.adsefid.com",
    HttpClient = httpClient,
    // Identify your own application; the SDK's default is "adsefid-dotnet/<version>".
    UserAgent = "my-billing-service/1.4 (+https://example.com)",
});

var info = await client.User.GetInfoAsync();
Console.WriteLine($"account {info.Name} ({info.AccountStatus})");
Console.WriteLine($"  credit left: {info.CreditLeft}");
if (info.Email is not null)
{
    Console.WriteLine($"  email: {info.Email}");
}

var lines = await client.User.GetLinesAsync();
Console.WriteLine($"\n{lines.Count} SMS line(s):");
foreach (var line in lines)
{
    var state = line.Enabled ? "enabled" : "disabled";
    Console.WriteLine($"  {line.LineNumber,-14} {line.LineName,-24} {state,-9} default selector: {line.LineSelector}");
}

var profiles = await client.User.GetProfilesAsync();
Console.WriteLine($"\n{profiles.Count} messenger profile(s):");
foreach (var profile in profiles)
{
    Console.WriteLine($"  {profile.Id} {profile.Name} ({profile.Messenger})");
}

// Templates are paged; take is capped at 100.
var page = await client.User.GetTemplatesAsync(skip: 0, take: 100);
Console.WriteLine($"\n{page.Total} template(s) (showing {page.Items.Count}):");
foreach (var item in page.Items)
{
    Console.WriteLine($"  {item.TemplateId,-24} {item.State,-16} {item.Parameters.Count} parameter(s)");
}

return 0;
