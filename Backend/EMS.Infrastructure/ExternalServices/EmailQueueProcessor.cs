using System.Diagnostics;
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
    // Ek saath max itne emails. Ek slow email baaki OTP ko rok nahi sakta.
    private const int MaxParallelSends = 4;

    private readonly BackgroundEmailQueue _backgroundEmailQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEmailStatusStore _emailStatusStore;
    private readonly ILogger<EmailQueueProcessor> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _gate = new(MaxParallelSends);

    public EmailQueueProcessor(BackgroundEmailQueue backgroundEmailQueue, IServiceScopeFactory serviceScopeFactory, IEmailStatusStore emailStatusStore, ILogger<EmailQueueProcessor> logger)
    {
        _backgroundEmailQueue = backgroundEmailQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _emailStatusStore = emailStatusStore;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(ex, "OTP email attempt {Attempt} failed. Retrying in {Delay}s...", attempt, delay.TotalSeconds);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _backgroundEmailQueue.Reader.ReadAllAsync(stoppingToken))
        {
            await _gate.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try { await ProcessJobAsync(job); }
                finally { _gate.Release(); }
            }, CancellationToken.None);
        }
    }

    private async Task ProcessJobAsync(OtpEmailJob job)
    {
        var queueWaitMs = (int)(DateTime.UtcNow - job.QueuedAtUtc).TotalMilliseconds;
        var sw = Stopwatch.StartNew();

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendOtpEmailAsync(job.ToEmail, job.OtpCode, job.IsResend, job.Purpose);
            });

            _emailStatusStore.Update(job.ToEmail, EmailDeliveryStatus.Sent);

            _logger.LogInformation(
                "OTP email handed to provider: To={Email} Purpose={Purpose} QueueWait={QueueWaitMs}ms ProviderCall={ProviderMs}ms",
                job.ToEmail, job.Purpose, queueWaitMs, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP email permanently failed for {Email} after all retries", job.ToEmail);
            _emailStatusStore.Update(job.ToEmail, EmailDeliveryStatus.Failed);
        }
    }
}