using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryTemplate.Infrastructure.Email;

public sealed class LoggingEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendLoanStartedAsync(LoanStartedEmail email)
    {
        logger.LogInformation(
            "Email from {FromAddress} to {To}: loan {LoanNumber} started with {BookCount} book(s)",
            emailOptions.Value.FromAddress,
            email.To,
            email.LoanNumber,
            email.BookCount);

        return Task.CompletedTask;
    }

    public Task SendOverdueReminderAsync(OverdueLoanProjection overdueLoan)
    {
        logger.LogInformation(
            "Email from {FromAddress} to {To}: loan {LoanNumber} has overdue book {BookTitle}",
            emailOptions.Value.FromAddress,
            overdueLoan.BorrowerEmail,
            overdueLoan.LoanNumber,
            overdueLoan.BookTitle);

        return Task.CompletedTask;
    }
}
