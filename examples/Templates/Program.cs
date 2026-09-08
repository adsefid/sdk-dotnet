// Listing templates and sending one, with exact numeric parameter values.
//
// A parameter the template declares as "number" may be sent either as a JSON
// number or as a JSON string, and the service substitutes a numeric string
// verbatim. That is the only way to keep a value's exact digits: "001234"
// keeps its leading zeros and "1.50" its trailing zero.
using Adsefid.Sdk;
using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Models.Common;
using Adsefid.Sdk.Sms.Models;

var apiKey = Environment.GetEnvironmentVariable("ADSEFID_API_KEY");
var lineNumber = Environment.GetEnvironmentVariable("ADSEFID_LINE_NUMBER");
if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(lineNumber))
{
    Console.Error.WriteLine("set ADSEFID_API_KEY and ADSEFID_LINE_NUMBER");
    return 1;
}

var client = new AdsefidClient(new AdsefidClientOptions { ApiKey = apiKey });

var page = await client.User.GetTemplatesAsync(TemplateState.Approved, take: 100);
if (page.Items.Count == 0)
{
    Console.Error.WriteLine("no approved templates on this account — create one in the adsefid.com panel first");
    return 1;
}

Console.WriteLine($"{page.Total} approved template(s):");
foreach (var item in page.Items)
{
    Console.WriteLine($"  {item.TemplateId,-24} {item.Content}");
}

var template = page.Items[0];

// Build one value per declared parameter. A number-typed parameter gets a
// decimal here, which round-trips exactly; a string would too.
var parameters = new Dictionary<string, TemplateParameterValue>();
foreach (var (name, kind) in template.Parameters)
{
    parameters[name] = kind == TemplateParameterType.String ? "Ali" : 1.50m;
}

var result = await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
{
    TemplateId = template.TemplateId,
    Parameters = parameters,
    Receptor = "09120000000",
    LineNumber = lineNumber,
    ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(10),
});

Console.WriteLine($"\nsent {result.MessageId}: {result.Status}");
Console.WriteLine($"  rendered: {result.Message}");
Console.WriteLine("  parameters echoed back:");
foreach (var (name, value) in result.Parameters)
{
    var rendered = value.IsNumber ? $"number {value.AsNumber}" : $"string \"{value.AsString}\"";
    Console.WriteLine($"    {name,-14} {rendered}");
}

// Which representation to reach for, shown without sending anything.
Console.WriteLine("\nchoosing a parameter value:");
foreach (var (why, value) in new (string, TemplateParameterValue)[]
         {
             ("an ordinary count", 2),
             ("a price where decimal precision is enough", 19.99m),
             ("an invoice number whose leading zeros matter", "001234"),
             ("an amount that must render as exactly 1.50", "1.50"),
         })
{
    Console.WriteLine($"  {why,-48} -> {value}");
}

return 0;
