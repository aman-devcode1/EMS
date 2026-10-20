using System.Threading.Channels;
using EMS.Core.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.IServices;

namespace EMS.Infrastructure.ExternalServices;

public class BackgroundEmailQueue : IBackgroundEmailQueue
{
    // Max 500 emails queue mein. Isse zyada ho to naya request saaf karan ke saath mana ho jata hai.
    private readonly Channel<OtpEmailJob> _channel = Channel.CreateBounded<OtpEmailJob>(
        new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        }
    );

    public void QueueOtpEmail(string toEmail, string otpCode, bool isResend, OtpPurpose purpose)
    {
        if(!_channel.Writer.TryWrite(new OtpEmailJob(toEmail, otpCode, isResend, purpose)))
        {
            throw new ExternalServiceException("Too many email requests are being processed right now. Please again in a minute.");
        }      
    }

    public ChannelReader<OtpEmailJob> Reader => _channel.Reader;
}