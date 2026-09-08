using System.Text.Json;
using Adsefid.Sdk.Json;
using Adsefid.Sdk.Models.Common;
using Xunit;

namespace Adsefid.Sdk.Tests;

/// <summary>
/// The round trip that makes leading zeros and exact decimals survive: a number-typed parameter
/// may travel as a JSON string, and the service substitutes such a value verbatim.
/// </summary>
public sealed class TemplateParameterValueTests
{
    private static string Serialize(TemplateParameterValue value) =>
        JsonSerializer.Serialize(value, AdsefidJsonContext.Default.TemplateParameterValue);

    private static TemplateParameterValue Deserialize(string json) =>
        JsonSerializer.Deserialize(json, AdsefidJsonContext.Default.TemplateParameterValue);

    [Theory]
    [InlineData("459122", "\"459122\"")]
    [InlineData("001234", "\"001234\"")]
    [InlineData("1.50", "\"1.50\"")]
    [InlineData("", "\"\"")]
    public void AStringSerializesAsAJsonString(string value, string expected)
    {
        Assert.Equal(expected, Serialize(value));
    }

    [Fact]
    public void NumbersSerializeAsJsonNumbers()
    {
        Assert.Equal("2", Serialize(2));
        Assert.Equal("-7", Serialize(-7));
        Assert.Equal("9007199254740993", Serialize(9007199254740993L));
        Assert.Equal("1.5", Serialize(1.5m));
        Assert.Equal("19.99", Serialize(19.99m));
    }

    /// <summary>
    /// Numbers are held as <see cref="decimal"/>, so an exact decimal survives a round trip that
    /// <see cref="double"/> would have perturbed.
    /// </summary>
    [Fact]
    public void ADecimalRoundTripsExactly()
    {
        var value = Deserialize("1.50");

        Assert.True(value.IsNumber);
        Assert.Equal(1.50m, value.AsNumber);
        Assert.Equal("1.50", Serialize(value));
    }

    [Fact]
    public void ALongIsNotTruncatedThroughADouble()
    {
        // 2^53 + 1 is the first integer a double cannot represent.
        const long beyondDoublePrecision = 9007199254740993L;

        var value = Deserialize(beyondDoublePrecision.ToString());

        Assert.Equal(beyondDoublePrecision, value.AsNumber);
    }

    [Fact]
    public void AccessorsReportTheHeldKind()
    {
        TemplateParameterValue text = "001234";
        Assert.False(text.IsNumber);
        Assert.True(text.HasValue);
        Assert.Equal("001234", text.AsString);
        Assert.Null(text.AsNumber);

        TemplateParameterValue number = 1.5m;
        Assert.True(number.IsNumber);
        Assert.True(number.HasValue);
        Assert.Equal(1.5m, number.AsNumber);
        Assert.Null(number.AsString);
    }

    /// <summary>
    /// A <see langword="default"/> instance is not a usable value. Serializing it would emit JSON
    /// null, which the service rejects and this type's own reader refuses, so it fails loudly here
    /// instead of shipping a wrong request.
    /// </summary>
    [Fact]
    public void ADefaultInstanceCannotBeSerialized()
    {
        var unset = default(TemplateParameterValue);

        Assert.False(unset.HasValue);
        Assert.Null(unset.AsString);
        Assert.Null(unset.AsNumber);
        Assert.Throws<JsonException>(() => Serialize(unset));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void OnlyAStringOrANumberCanBeRead(string json)
    {
        Assert.Throws<JsonException>(() => Deserialize(json));
    }

    [Fact]
    public void ImplicitConversionsCoverTheUsefulNumericTypes()
    {
        Assert.Equal(2m, ((TemplateParameterValue)2).AsNumber);
        Assert.Equal(2m, ((TemplateParameterValue)2L).AsNumber);
        Assert.Equal(1.5m, ((TemplateParameterValue)1.5m).AsNumber);
        Assert.Equal(1.5m, ((TemplateParameterValue)1.5d).AsNumber);
        Assert.Equal("x", ((TemplateParameterValue)"x").AsString);
    }

    [Fact]
    public void ToStringRendersTheHeldValue()
    {
        Assert.Equal("001234", ((TemplateParameterValue)"001234").ToString());
        Assert.Equal("1.50", ((TemplateParameterValue)1.50m).ToString());
    }
}
