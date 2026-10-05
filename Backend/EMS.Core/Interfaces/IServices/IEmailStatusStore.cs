using EMS.Core.Enums;
namespace EMS.Core.Interfaces.IServices;

public record EmailStatusSnapshot(EmailDeliveryStatus Status, DateTime QueuedAtUtc);

public interface IEmailStatusStore
{
    void MarkQueued(string email);
    void Update(string email, EmailDeliveryStatus status); // sirf tab jab MarkQueued ho chuka ho
    EmailStatusSnapshot Get(string email);
}