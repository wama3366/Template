using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Events;

namespace LibraryTemplate.Domain.Aggregates.Loans;

public sealed record LoanStartedEvent(
    LoanId LoanId,
    BranchId BranchId,
    UserId BorrowerId,
    string LoanNumber,
    IReadOnlyCollection<BookId> BookIds) : IDomainEvent
{
    public DateTimeOffset RaisedAt { get; init; } = DateTimeOffset.UtcNow;
}
