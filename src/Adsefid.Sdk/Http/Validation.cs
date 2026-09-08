using System.Text.RegularExpressions;
using Adsefid.Sdk.Exceptions;

namespace Adsefid.Sdk.Http;

internal static partial class Validation
{
    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9\-_.:]{0,34}[A-Za-z0-9])?$")]
    internal static partial Regex LocalIdPattern();

    /// <summary>
    /// Validates an optional local id. The service normalizes a blank value to
    /// "not supplied" before validating, so <see langword="null"/>, empty and
    /// whitespace-only are all accepted and simply omitted from the request.
    /// </summary>
    public static void ValidateLocalId(string? localId, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(localId))
        {
            return;
        }

        if (!LocalIdPattern().IsMatch(localId))
        {
            throw new AdsefidValidationException(
                $"'{fieldName}' must be 1-36 ASCII letters/digits, with '-', '_', '.', ':' allowed only between the first and last character.");
        }
    }

    public static void RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new AdsefidValidationException($"'{fieldName}' is required.");
        }
    }

    public static void RequireNonEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new AdsefidValidationException($"'{fieldName}' is required.");
        }
    }

    /// <summary>
    /// Enforces a maximum length in UTF-16 code units, which is what the service
    /// counts. <see cref="string.Length"/> is already that measure in .NET, so a
    /// character outside the Basic Multilingual Plane correctly costs two.
    /// </summary>
    public static void RequireMaxLength(string value, int maxLength, string fieldName)
    {
        if (value.Length > maxLength)
        {
            throw new AdsefidValidationException($"'{fieldName}' must be at most {maxLength} characters long.");
        }
    }

    public static void RequireNonEmptyCollection<T>(IReadOnlyCollection<T>? collection, string fieldName)
    {
        if (collection is null || collection.Count == 0)
        {
            throw new AdsefidValidationException($"'{fieldName}' must contain at least one item.");
        }
    }

    public static void RequireInRange(int value, int min, int max, string fieldName)
    {
        if (value < min || value > max)
        {
            throw new AdsefidValidationException($"'{fieldName}' must be between {min} and {max}.");
        }
    }

    public static void RequireAtLeastOne(bool firstProvided, bool secondProvided, string message)
    {
        if (!firstProvided && !secondProvided)
        {
            throw new AdsefidValidationException(message);
        }
    }

    public static void RequireCombinedCountAtMost(int count1, int count2, int max, string message)
    {
        if (count1 + count2 > max)
        {
            throw new AdsefidValidationException(message);
        }
    }
}
