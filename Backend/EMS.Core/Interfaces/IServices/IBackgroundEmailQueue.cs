using EMS.Core.Enums;

namespace EMS.Core.Interfaces.IServices;

public interface IBackgroundEmailQueue
{
    void QueueOtpEmail(string toEmail, string otpCode, bool isResend, OtpPurpose purpose);
}