using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Dependencies.Persistence;

namespace LibraryTemplate.Application.Dependencies.ExternalServices;

public interface IEmailSender
{
    Task SendLoanStartedAsync(LoanStartedEmail email);
    Task SendOverdueReminderAsync(OverdueLoanProjection overdueLoan);
}

public sealed record LoanStartedEmail(
    string To,
    string BorrowerName,
    string LoanNumber,
    int BookCount);
