using System.Threading.Channels;
using EMS.Core.Enums;
using EMS.Core.Interfaces.IServices;

namespace EMS.Infrastructure.ExternalServices;

public class BackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly Channel<OtpEmailJob> _channel = Channel.CreateUnbounded<OtpEmailJob>();

    public void QueueOtpEmail(string toEmail, string otpCode, bool isResend, OtpPurpose purpose)
        => _channel.Writer.TryWrite(new OtpEmailJob(toEmail, otpCode, isResend, purpose));

    public ChannelReader<OtpEmailJob> Reader => _channel.Reader;
}