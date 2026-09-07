using System.Text.Json.Serialization;

namespace Adsefid.Sdk.User.Models;

/// <summary>Response from <see cref="UserResource.GetInfoAsync"/>.</summary>
public sealed class UserInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Remaining account credit/balance.</summary>
    [JsonPropertyName("credit_left")]
    public required long CreditLeft { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("account_status")]
    public required string AccountStatus { get; init; }
}
