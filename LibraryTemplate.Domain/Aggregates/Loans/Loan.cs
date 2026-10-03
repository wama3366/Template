using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Domain.Aggregates.Loans;

public sealed class Loan : AggregateRoot<LoanId>
{
    private readonly List<LoanLine> _lines = [];

    private Loan()
    {
        LoanNumber = LoanNumber.Create("LN-000000");
    }

    private Loan(LoanId id, BranchId branchId, UserId borrowerId, LoanNumber loanNumber, DateOnly borrowedOn) : base(id)
    {
        BranchId = branchId;
        BorrowerId = borrowerId;
        LoanNumber = loanNumber;
        BorrowedOn = borrowedOn;
        Status = LoanStatus.Active;
    }

    public BranchId BranchId { get; private set; }
    public UserId BorrowerId { get; private set; }
    public LoanNumber LoanNumber { get; private set; }
    public DateOnly BorrowedOn { get; private set; }
    public LoanStatus Status { get; private set; }
    public IReadOnlyCollection<LoanLine> Lines => _lines.AsReadOnly();

    public static Loan Start(
        BranchId branchId,
        UserId borrowerId,
        BorrowerCategory borrowerCategory,
        IReadOnlyCollection<Book> books,
        DateOnly borrowedOn)
    {
        if (books.Count == 0)
        {
            throw new DomainException("Loan.Empty", "At least one book is required to start a loan.");
        }

        foreach (var book in books)
        {
            if (book.IsBlocked)
            {
                throw new DomainException("Loan.BookBlocked", $"'{book.Title}' is blocked and cannot be borrowed.");
            }

            if (borrowerCategory == BorrowerCategory.Child && book.AgeRestriction == AgeRestriction.AdultOnly)
            {
                throw new DomainException("Loan.AgeRestrictedBook", $"'{book.Title}' cannot be borrowed by a child.");
            }
        }

        var loan = new Loan(
            LoanId.New(),
            branchId,
            borrowerId,
            LoanNumber.New(),
            borrowedOn);

        foreach (var book in books)
        {
            loan._lines.Add(LoanLine.Create(loan.Id, book.Id, book.Title, borrowedOn.AddDays(14)));
        }

        loan.Raise(new LoanStartedEvent(
            loan.Id,
            loan.BranchId,
            loan.BorrowerId,
            loan.LoanNumber.Value,
            loan._lines.Select(line => line.BookId).ToArray()));

        return loan;
    }
}

public readonly record struct LoanId(Guid Value)
{
    public static LoanId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
