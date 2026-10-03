using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application.Events;

public sealed class LoanStartedEventHandler(
    IUserRepository userRepository,
    IEmailSender emailSender,
    ILogger<LoanStartedEventHandler> logger) : IDomainEventHandler<LoanStartedEvent>
{
    public async Task HandleAsync(LoanStartedEvent domainEvent)
    {
        var borrower = await userRepository.FindByIdAsync(domainEvent.BorrowerId);

        if (borrower is null)
        {
            logger.LogWarning(
                "LoanStartedEvent email skipped because borrower {BorrowerId} was not found",
                domainEvent.BorrowerId);
            return;
        }

        await emailSender.SendLoanStartedAsync(new LoanStartedEmail(
            borrower.Email,
            borrower.DisplayName,
            domainEvent.LoanNumber,
            domainEvent.BookIds.Count));
    }
}
