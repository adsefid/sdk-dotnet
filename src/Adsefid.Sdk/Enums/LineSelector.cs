namespace Adsefid.Sdk.Enums;

/// <summary>
/// Selects which of an account's lines/tariffs is used to send a message, and whether it is billed
/// per message sent ("SendBased") or per message actually delivered ("DeliverBased").
/// </summary>
public enum LineSelector
{
    PromotionalSendBased = 0,
    PromotionalDeliverBased = 1,
    BulkServiceSendBased = 2,
    BulkServiceDeliverBased = 3,
    CustomerClubServiceSendBased = 4,
    CustomerClubServiceDeliverBased = 5,
}
