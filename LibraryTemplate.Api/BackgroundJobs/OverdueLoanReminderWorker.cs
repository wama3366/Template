using LibraryTemplate.Application.BackgroundJobs;
using LibraryTemplate.Api.Options;
using Microsoft.Extensions.Options;

namespace LibraryTemplate.Api.BackgroundJobs;

public sealed class OverdueLoanReminderWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundJobOptions> backgroundJobOptions,
    ILogger<OverdueLoanReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!backgroundJobOptions.Value.EnableOverdueReminders)
        {
            logger.LogInformation("Overdue loan reminders are disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendRemindersAsync();
            await Task.Delay(TimeSpan.FromSeconds(backgroundJobOptions.Value.OverdueReminderIntervalSeconds), stoppingToken);
        }
    }

    private async Task SendRemindersAsync()
    {
        // Must be injected this way because the BackgroundService is a singleton and the IOverdueLoanReminderJob
        // is a scoped dependency.
        using var scope = serviceScopeFactory.CreateScope();
        var overdueLoanReminderJob = scope.ServiceProvider.GetRequiredService<IOverdueLoanReminderJob>();

        var reminderCount = await overdueLoanReminderJob.SendRemindersAsync(
            backgroundJobOptions.Value.OverdueReminderBatchSize);

        logger.LogInformation("Processed {OverdueLoanCount} overdue loan reminder(s)", reminderCount);
    }
}
