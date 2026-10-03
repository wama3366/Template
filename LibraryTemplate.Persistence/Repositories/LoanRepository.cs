using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Dependencies.Persistence;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class LoanRepository(LibraryDbContext dbContext) : ILoanRepository
{
    public void Add(Loan loan) => dbContext.Loans.Add(loan);
}
