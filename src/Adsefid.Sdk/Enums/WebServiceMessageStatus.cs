namespace Adsefid.Sdk.Enums;

/// <summary>
/// The delivery/lifecycle status of a single SMS or Messenger message, as returned by send, status,
/// and cancel responses and by status webhooks. The underlying numeric values match the adsefid.com
/// API's status codes.
/// </summary>
public enum WebServiceMessageStatus
{
    Scheduled = 1000,
    Sending = 1001,
    Delivered = 1002,
    Undelivered = 1003,
    Canceled = 1004,
    SentToOperator = 1005,
    Blacklisted = 1006,
    ProviderError = 1007,
    PendingApproval = 1008,
    Rejected = 1009,
    InvalidSender = 1010,
    InvalidAttachment = 1011,
    ForbiddenWord = 1012,
    LinkNotAllowed = 1013,
    InvalidReceiver = 1014,
    Undeliverable = 1015,
    SenderLimitReached = 1016,
    Unknown = 1999,
}
