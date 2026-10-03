using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Application.Events;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LibraryTemplate.Application.Tests;

public sealed class LoanStartedEmailEventHandlerTests
{
    [Fact]
    public async Task Email_handler_sends_receipt_for_loan_started()
    {
        // Arrange
        var borrowerId = UserId.New();
        var users = Substitute.For<IUserRepository>();
        var emailSender = Substitute.For<IEmailSender>();
        var domainEvent = new LoanStartedEvent(
            LoanId.New(),
            BranchId.New(),
            borrowerId,
            "LN-1",
            [BookId.New()]);

        users.FindByIdAsync(borrowerId)
            .Returns(User.Load(
                borrowerId,
                "Adult Borrower",
                "adult@example.test",
                [UserRole.Borrower]));

        var handler = new LoanStartedEventHandler(
            users,
            emailSender,
            NullLogger<LoanStartedEventHandler>.Instance);

        // Act
        await handler.HandleAsync(domainEvent);

        // Assert
        await emailSender.Received(1).SendLoanStartedAsync(
            Arg.Is<LoanStartedEmail>(email =>
                email.To == "adult@example.test"
                && email.LoanNumber == "LN-1"
                && email.BookCount == 1));
    }
}
