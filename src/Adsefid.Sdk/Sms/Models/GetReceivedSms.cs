using System.Text.Json.Serialization;

namespace Adsefid.Sdk.Sms.Models;

/// <summary>Response from <see cref="SmsResource.GetReceivedAsync"/>.</summary>
public sealed class GetReceivedSmsResponse
{
    [JsonPropertyName("messages")]
    public required IReadOnlyList<ReceivedSmsMessage> Messages { get; init; }
}

/// <summary>A single inbound SMS message.</summary>
public sealed class ReceivedSmsMessage
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("line_number")]
    public required string LineNumber { get; init; }

    [JsonPropertyName("receive_date")]
    public required DateTimeOffset ReceiveDate { get; init; }

    /// <summary>The phone number the message was sent from.</summary>
    [JsonPropertyName("sender")]
    public required string Sender { get; init; }
}
