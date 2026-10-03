using LibraryTemplate.Domain.Aggregates.Loans;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface ILoanRepository
{
    void Add(Loan loan);
}
