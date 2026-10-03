using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class LoanOverviewRepository(LibraryDbContext dbContext) : ILoanOverviewRepository
{
    public async Task<IReadOnlyCollection<BorrowedLoanProjection>> ListBorrowedForBranchAsync(BranchId branchId) =>
        await BorrowedLoans(branchId).ToArrayAsync();

    public async Task<IReadOnlyCollection<BorrowedLoanProjection>> ListBorrowedAsync() =>
        await BorrowedLoans(branchId: null).ToArrayAsync();

    public async Task<IReadOnlyCollection<OverdueLoanProjection>> ListOverdueAsync(
        DateOnly today,
        int take) =>
        await (
            from loan in dbContext.Loans.AsNoTracking()
            from line in loan.Lines
            join borrower in dbContext.Users.AsNoTracking() on loan.BorrowerId equals borrower.Id
            where loan.Status == LoanStatus.Active
                && line.ReturnedOn == null
                && line.DueOn < today
            orderby line.DueOn
            select new OverdueLoanProjection(
                loan.Id,
                loan.LoanNumber.Value,
                loan.BranchId,
                borrower.Id,
                borrower.DisplayName,
                borrower.Email,
                line.BookTitle,
                line.DueOn))
            .Take(take)
            .ToArrayAsync();

    private IQueryable<BorrowedLoanProjection> BorrowedLoans(BranchId? branchId) =>
        from loan in dbContext.Loans.AsNoTracking()
        from line in loan.Lines
        join borrower in dbContext.Users.AsNoTracking() on loan.BorrowerId equals borrower.Id
        where loan.Status == LoanStatus.Active
            && line.ReturnedOn == null
            && (branchId == null || loan.BranchId == branchId)
        orderby loan.BorrowedOn descending, line.BookTitle
        select new BorrowedLoanProjection(
            loan.Id,
            loan.LoanNumber.Value,
            loan.BranchId,
            borrower.Id,
            borrower.DisplayName,
            borrower.Email,
            line.BookTitle,
            line.DueOn);
}
