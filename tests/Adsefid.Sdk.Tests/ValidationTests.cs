using System.Text.Json;
using Adsefid.Sdk.Exceptions;
using Adsefid.Sdk.Http;
using Adsefid.Sdk.Tests.Infrastructure;
using Xunit;

namespace Adsefid.Sdk.Tests;

public sealed class ValidationTests
{
    /// <summary>
    /// The golden table is shared byte-for-byte with the sibling SDK repositories, so all five
    /// agree on what a local id may be.
    /// </summary>
    public static TheoryData<string, bool, string> LocalIdCases()
    {
        var data = new TheoryData<string, bool, string>();
        foreach (var element in Fixtures.Json("validation/local_ids.json").EnumerateArray())
        {
            data.Add(
                element.GetProperty("value").GetString()!,
                element.GetProperty("valid").GetBoolean(),
                element.GetProperty("why").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LocalIdCases))]
    public void LocalIdGoldenTable(string value, bool valid, string why)
    {
        var validate = () => Validation.ValidateLocalId(value, "local_id");

        if (valid)
        {
            var unexpected = Record.Exception(validate);
            Assert.True(unexpected is null, $"{value.Length} chars should be valid ({why}), got {unexpected?.Message}");
        }
        else
        {
            var thrown = Record.Exception(validate);
            Assert.True(thrown is AdsefidValidationException, $"{value.Length} chars should be rejected ({why})");
        }
    }

    [Fact]
    public void ANullLocalIdIsNotSupplied()
    {
        Validation.ValidateLocalId(null, "local_id");
    }

    [Fact]
    public void TheFieldNameReachesTheMessage()
    {
        var exception = Assert.Throws<AdsefidValidationException>(
            () => Validation.ValidateLocalId("-bad", "receptors[0].LocalId"));

        Assert.Contains("receptors[0].LocalId", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The service counts its length limits in UTF-16 code units, which is exactly what
    /// <see cref="string.Length"/> measures — so .NET agrees with it for free, including for
    /// characters outside the Basic Multilingual Plane.
    /// </summary>
    [Theory]
    [InlineData(900, 'a', false)]
    [InlineData(901, 'a', true)]
    [InlineData(900, 'س', false)]
    [InlineData(901, 'س', true)]
    public void MaxLengthCountsUtf16CodeUnits(int count, char character, bool rejected)
    {
        var value = new string(character, count);
        var validate = () => Validation.RequireMaxLength(value, Limits.SmsMessageMaxLength, "message");

        if (rejected)
        {
            Assert.Throws<AdsefidValidationException>(validate);
        }
        else
        {
            validate();
        }
    }

    [Theory]
    [InlineData(450, false)]
    [InlineData(451, true)]
    [InlineData(900, true)]
    public void ANonBmpCharacterCostsTwoCodeUnits(int emojiCount, bool rejected)
    {
        // Each emoji is one Unicode code point but a surrogate pair in UTF-16,
        // so 450 of them exactly fill a 900-unit budget.
        var value = string.Concat(Enumerable.Repeat("😀", emojiCount));
        Assert.Equal(emojiCount * 2, value.Length);

        var validate = () => Validation.RequireMaxLength(value, Limits.SmsMessageMaxLength, "message");

        if (rejected)
        {
            Assert.Throws<AdsefidValidationException>(validate);
        }
        else
        {
            validate();
        }
    }

    [Fact]
    public void TheSharedLimitsTableMatchesWhatThisSdkEnforces()
    {
        var limits = Fixtures.Json("validation/limits.json");

        Assert.Equal(Limits.SmsMessageMaxLength, limits.GetProperty("sms_message_max_length").GetInt32());
        Assert.Equal(Limits.MessengerMessageMaxLength, limits.GetProperty("messenger_message_max_length").GetInt32());
        Assert.Equal(Limits.CombinedStatusIdsMax, limits.GetProperty("combined_status_ids_max").GetInt32());
        Assert.Equal(Limits.ReceiveCountMin, limits.GetProperty("receive_count_min").GetInt32());
        Assert.Equal(Limits.ReceiveCountMax, limits.GetProperty("receive_count_max").GetInt32());
        Assert.Equal(Limits.TemplatesTakeMin, limits.GetProperty("templates_take_min").GetInt32());
        Assert.Equal(Limits.TemplatesTakeMax, limits.GetProperty("templates_take_max").GetInt32());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RequireNonEmptyRejectsBlank(string? value)
    {
        Assert.Throws<AdsefidValidationException>(() => Validation.RequireNonEmpty(value, "receptor"));
    }

    [Fact]
    public void RequireNonEmptyRejectsAnEmptyGuid()
    {
        Assert.Throws<AdsefidValidationException>(() => Validation.RequireNonEmpty(Guid.Empty, "messageId"));
        Validation.RequireNonEmpty(Guid.NewGuid(), "messageId");
    }

    [Fact]
    public void RequireNonEmptyCollectionRejectsNullAndEmpty()
    {
        Assert.Throws<AdsefidValidationException>(() => Validation.RequireNonEmptyCollection<string>(null, "receptors"));
        Assert.Throws<AdsefidValidationException>(() => Validation.RequireNonEmptyCollection(Array.Empty<string>(), "receptors"));
        Validation.RequireNonEmptyCollection(new[] { "a" }, "receptors");
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(250, false)]
    [InlineData(499, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    [InlineData(500, true)]
    public void RequireInRangeIsInclusive(int value, bool rejected)
    {
        var validate = () => Validation.RequireInRange(value, Limits.ReceiveCountMin, Limits.ReceiveCountMax, "count");

        if (rejected)
        {
            Assert.Throws<AdsefidValidationException>(validate);
        }
        else
        {
            validate();
        }
    }

    [Fact]
    public void RequireAtLeastOneRejectsWhenEveryListIsEmpty()
    {
        Assert.Throws<AdsefidValidationException>(
            () => Validation.RequireAtLeastOne(false, false, "At least one is required."));

        Validation.RequireAtLeastOne(true, false, "At least one is required.");
        Validation.RequireAtLeastOne(false, true, "At least one is required.");
    }

    [Fact]
    public void RequireCombinedCountAtMostIsInclusive()
    {
        Validation.RequireCombinedCountAtMost(1000, 1000, Limits.CombinedStatusIdsMax, "too many");

        Assert.Throws<AdsefidValidationException>(
            () => Validation.RequireCombinedCountAtMost(1000, 1001, Limits.CombinedStatusIdsMax, "too many"));
    }
}
