using FluentAssertions;
using LibraryTemplate.Application.BackgroundJobs;
using LibraryTemplate.Application.Dependencies.ExternalServices;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LibraryTemplate.Application.Tests;

public sealed class OverdueLoanReminderJobTests
{
    [Fact]
    public async Task Sends_one_reminder_for_each_overdue_loan()
    {
        // Arrange
        var today = new DateOnly(2026, 6, 18);
        var overdueLoan = new OverdueLoanProjection(
            LoanId.New(),
            "LN-1",
            BranchId.New(),
            UserId.New(),
            "Adult Borrower",
            "adult@example.test",
            "The Clean Shelf",
            today.AddDays(-1));

        var loanOverviewRepository = Substitute.For<ILoanOverviewRepository>();
        loanOverviewRepository.ListOverdueAsync(today, 10)
            .Returns([overdueLoan]);

        var clock = Substitute.For<IClock>();
        clock.Today.Returns(today);

        var emailSender = Substitute.For<IEmailSender>();
        var job = new OverdueLoanReminderJob(
            loanOverviewRepository,
            clock,
            emailSender,
            NullLogger<OverdueLoanReminderJob>.Instance);

        // Act
        var reminderCount = await job.SendRemindersAsync(batchSize: 10);

        // Assert
        reminderCount.Should().Be(1);
        await loanOverviewRepository.Received(1).ListOverdueAsync(today, 10);
        await emailSender.Received(1).SendOverdueReminderAsync(overdueLoan);
    }
}
