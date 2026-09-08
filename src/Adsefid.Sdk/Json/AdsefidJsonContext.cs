using System.Text.Json.Serialization;
using Adsefid.Sdk.Messenger.Models;
using Adsefid.Sdk.Models.Common;
using Adsefid.Sdk.Sms.Models;
using Adsefid.Sdk.User.Models;
using Adsefid.Sdk.Webhooks;

namespace Adsefid.Sdk.Json;

// WhenWritingNull keeps an omitted optional out of the request body entirely,
// matching the sibling SDKs — otherwise every unset nullable would travel as an
// explicit null. It affects writing only, so response parsing is unchanged.
[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ErrorEnvelope))]
[JsonSerializable(typeof(SendSingleSmsRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendSingleSmsResponse>))]
[JsonSerializable(typeof(SendBulkSmsRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendBulkSmsResponse>))]
[JsonSerializable(typeof(SendP2PSmsRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendP2PSmsResponse>))]
[JsonSerializable(typeof(SendTemplateSmsRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendTemplateSmsResponse>))]
[JsonSerializable(typeof(ResponseEnvelope<GetSmsStatusResponse>))]
[JsonSerializable(typeof(CancelSmsRequest))]
[JsonSerializable(typeof(ResponseEnvelope<CancelSmsResponse>))]
[JsonSerializable(typeof(ResponseEnvelope<GetReceivedSmsResponse>))]
[JsonSerializable(typeof(SendSingleMessengerRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendSingleMessengerResponse>))]
[JsonSerializable(typeof(SendBulkMessengerRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendBulkMessengerResponse>))]
[JsonSerializable(typeof(SendP2PMessengerRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendP2PMessengerResponse>))]
[JsonSerializable(typeof(ResponseEnvelope<UploadMessengerFileResponse>))]
[JsonSerializable(typeof(CancelMessengerRequest))]
[JsonSerializable(typeof(ResponseEnvelope<CancelMessengerResponse>))]
[JsonSerializable(typeof(SendTemplateMessengerRequest))]
[JsonSerializable(typeof(ResponseEnvelope<SendTemplateMessengerResponse>))]
[JsonSerializable(typeof(ResponseEnvelope<GetMessengerStatusResponse>))]
[JsonSerializable(typeof(ResponseEnvelope<UserInfo>))]
[JsonSerializable(typeof(ResponseEnvelope<List<UserLine>>))]
[JsonSerializable(typeof(ResponseEnvelope<List<UserProfile>>))]
[JsonSerializable(typeof(ResponseEnvelope<GetUserTemplatesResponse>))]
[JsonSerializable(typeof(RawWebhookEnvelope))]
[JsonSerializable(typeof(List<ReceivedMessageItem>))]
[JsonSerializable(typeof(List<StatusUpdateItem>))]
internal partial class AdsefidJsonContext : JsonSerializerContext
{
}
