using EMS.Core.Enums;
using EMS.Core.Interfaces.ExternalServices;
using EMS.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace EMS.Infrastructure.ExternalServices;

public class EmailQueueProcessor : BackgroundService
{
    private readonly BackgroundEmailQueue _backgroundEmailQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEmailStatusStore _emailStatusStore;
    private readonly ILogger<EmailQueueProcessor> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailQueueProcessor(BackgroundEmailQueue backgroundEmailQueue, IServiceScopeFactory serviceScopeFactory, IEmailStatusStore emailStatusStore, ILogger<EmailQueueProcessor> logger)
    {
        _backgroundEmailQueue = backgroundEmailQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _emailStatusStore = emailStatusStore;
        _logger = logger;

        // Polly - sirf background worker ke andar, login request ko kabhi block nahi karta.
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),   // 2s, 4s, 8s...
                onRetry: (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(ex, "OTP email attempt { Attempt } falied. Retrying in { Delay }s...", attempt, delay.TotalSeconds);
                }
            );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _backgroundEmailQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendOtpEmailAsync(job.ToEmail, job.OtpCode, job.IsResend, job.Purpose);
                });
                _emailStatusStore.Update(job.ToEmail, EmailDeliveryStatus.Sent);
            }
            catch (Exception ex)
            {
                // 3 retries fail - permanently give up.
                _logger.LogError(ex, "Otp email permanently failed for { Email } after all retries", job.ToEmail);
                _emailStatusStore.Update(job.ToEmail, EmailDeliveryStatus.Failed);
                // "Resend OTP" se dobara try.
            }
        }
    }
}