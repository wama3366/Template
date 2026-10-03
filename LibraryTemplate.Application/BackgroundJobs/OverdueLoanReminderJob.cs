using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application.BackgroundJobs;

public interface IOverdueLoanReminderJob
{
    Task<int> SendRemindersAsync(int batchSize);
}

public sealed class OverdueLoanReminderJob(
    ILoanOverviewRepository loanOverviewRepository,
    IClock clock,
    IEmailSender emailSender,
    ILogger<OverdueLoanReminderJob> logger) : IOverdueLoanReminderJob
{
    public async Task<int> SendRemindersAsync(int batchSize)
    {
        var overdueLoans = await loanOverviewRepository.ListOverdueAsync(clock.Today, batchSize);

        foreach (var overdueLoan in overdueLoans)
        {
            await emailSender.SendOverdueReminderAsync(overdueLoan);
        }

        logger.LogInformation("Sent {OverdueLoanCount} overdue loan reminder(s)", overdueLoans.Count);

        return overdueLoans.Count;
    }
}
