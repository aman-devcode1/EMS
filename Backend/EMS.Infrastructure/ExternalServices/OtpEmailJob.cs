using EMS.Core.Enums;

namespace EMS.Infrastructure.ExternalServices;

public record OtpEmailJob(string ToEmail, string OtpCode, bool IsResend, OtpPurpose Purpose);