using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.BackgroundServices;

public class EmployeeCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmployeeCleanupService> _logger;

    // Har 24 ghante mein ek baar check karega
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public EmployeeCleanupService(IServiceProvider serviceProvider, ILogger<EmployeeCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 🔥 BackgroundService khud "Singleton" hoti hai, lekin AppDbContext "Scoped" hai —
                // isliye direct inject nahi kar sakte. Har baar ek naya "scope" banate hain.
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var expiredEmployees = await context.Employees
                    .Where(e => e.ScheduledForDeletionAt != null && e.ScheduledForDeletionAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredEmployees.Count > 0)
                {
                    context.Employees.RemoveRange(expiredEmployees);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Auto-deleted {Count} expired employee record(s).", expiredEmployees.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up expired employee records.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}