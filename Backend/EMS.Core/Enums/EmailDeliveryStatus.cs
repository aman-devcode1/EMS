namespace EMS.Core.Enums;

public enum EmailDeliveryStatus
{
    Unknown = 0,
    Queued, Sent,
    Delivered,
    Deferred,
    Failed
}
