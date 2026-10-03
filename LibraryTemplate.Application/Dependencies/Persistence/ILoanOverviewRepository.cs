using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Application.Dependencies.Persistence;

// Repos that return custom read shapes are defined in the application layer
public interface ILoanOverviewRepository
{
    Task<IReadOnlyCollection<BorrowedLoanProjection>> ListBorrowedForBranchAsync(BranchId branchId);
    Task<IReadOnlyCollection<BorrowedLoanProjection>> ListBorrowedAsync();
    Task<IReadOnlyCollection<OverdueLoanProjection>> ListOverdueAsync(DateOnly today, int take);
}

// Custom read shapes
#region Projections

public sealed record BorrowedLoanProjection(
    LoanId LoanId,
    string LoanNumber,
    BranchId BranchId,
    UserId BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    DateOnly DueOn);

public sealed record OverdueLoanProjection(
    LoanId LoanId,
    string LoanNumber,
    BranchId BranchId,
    UserId BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    DateOnly DueOn);

#endregion Projections
