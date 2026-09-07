using Adsefid.Sdk;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Sms.Models;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY")
    ?? throw new InvalidOperationException("ADSEFID_API_KEY is not set.");

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });

try
{
    var result = await client.Sms.SendSingleAsync(new SendSingleSmsRequest
    {
        Receptor = "98912****567",
        LineNumber = "3000xxxx",
        Message = "Hello from Adsefid.Sdk",
        LocalId = "quickstart-example",
    });

    Console.WriteLine($"Sent: message_id={result.MessageId} status={result.Status} cost={result.Cost}");
}
catch (AdsefidValidationException ex)
{
    Console.WriteLine($"Invalid request: {ex.Message}");
}
catch (AdsefidRateLimitException ex)
{
    Console.WriteLine($"Rate limited: {ex.Name} ({(int)ex.Code}), HTTP {ex.HttpStatusCode}");
}
catch (AdsefidApiException ex)
{
    var details = ex.Details is { } d ? d.GetRawText() : "(none)";
    Console.WriteLine($"API error {ex.Name} ({(int)ex.Code}), HTTP {ex.HttpStatusCode}, details={details}");
}
