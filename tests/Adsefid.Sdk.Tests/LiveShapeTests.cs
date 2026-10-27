using Adsefid.Sdk.Enums;
using Adsefid.Sdk.Models.Common;
using Adsefid.Sdk.Sms.Models;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

/// <summary>
/// Fixtures shaped like real gateway responses: 7-digit fractional seconds, an empty-string
/// description, null account fields, long decimal money and "+98" receptors.
/// </summary>
public sealed class LiveShapeTests
{
    [Fact]
    public async Task AccountInfoKeepsNullFieldsAndTheExactCredit()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/user.get_info.live.json");

        var info = await client.User.GetInfoAsync();

        Assert.Null(info.CompanyName);
        Assert.Null(info.Email);
        Assert.Equal(5877471300.00000m, info.CreditLeft);
    }

    [Fact]
    public async Task TemplatesParseSevenDigitTimestampsAndAnEmptyDescription()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/user.get_templates.live.json");

        var item = (await client.User.GetTemplatesAsync()).Items[0];

        Assert.Equal(string.Empty, item.Description);
        Assert.Equal(4030292, item.CreatedAt.Ticks % TimeSpan.TicksPerSecond);
    }

    [Fact]
    public async Task TemplateSendKeepsTheLongDecimalCostAndANormalizedReceptor()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.send_template.live.json");

        var response = await client.Sms.SendTemplateAsync(new SendTemplateSmsRequest
        {
            TemplateId = "otp",
            Parameters = new Dictionary<string, TemplateParameterValue> { ["otp_code"] = "001234" },
            Receptor = "989120000000",
            LineNumber = "983000",
        });

        Assert.Equal(1129m, response.Cost);
        Assert.Equal("+989120000000", response.Receptor);
        Assert.Null(response.ExpiryDate);
        Assert.NotNull(response.SendTime);
    }

    [Fact]
    public async Task StatusParsesASevenDigitDeliveryTime()
    {
        var (client, _) = TestClient.RespondingWithFixture("envelopes/sms.get_status.live.json");

        var item = (await client.Sms.GetStatusAsync(localIds: ["order-10001"])).Receptors[0];

        Assert.Equal(WebServiceMessageStatus.Undelivered, item.Status);
        Assert.NotNull(item.DeliveryTime);
    }
}
