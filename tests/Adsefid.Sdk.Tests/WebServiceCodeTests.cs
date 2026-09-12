using Adsefid.Sdk.Enums;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class WebServiceCodeTests
{
    [Theory]
    [InlineData(1000, WebServiceMessageStatus.Scheduled, null)]
    [InlineData(1002, WebServiceMessageStatus.Delivered, null)]
    [InlineData(2025, null, WebServiceResponseCode.ReceptorBlacklisted)]
    [InlineData(2046, null, WebServiceResponseCode.InvalidMessageIds)]
    [InlineData(2047, null, WebServiceResponseCode.FileTooLarge)]
    [InlineData(2014, null, WebServiceResponseCode.InvalidReceptor)]
    // Codes this SDK does not know yet map to neither view, but stay readable as the raw int.
    [InlineData(1500, null, null)]
    [InlineData(2999, null, null)]
    [InlineData(0, null, null)]
    public void SplitsAWebServiceCodeByRange(int code, WebServiceMessageStatus? status, WebServiceResponseCode? error)
    {
        Assert.Equal(status, WebServiceCode.AsMessageStatus(code));
        Assert.Equal(error, WebServiceCode.AsErrorCode(code));
        Assert.Equal(code is >= 1000 and < 2000, WebServiceCode.IsMessageStatus(code));
        Assert.Equal(code >= 2000, WebServiceCode.IsErrorCode(code));
    }
}
