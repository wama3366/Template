using LibraryTemplate.Domain.Aggregates.Books;

namespace LibraryTemplate.Domain.Aggregates.Loans;

public sealed class LoanLine
{
    private LoanLine()
    {
        BookTitle = string.Empty;
    }

    private LoanLine(LoanLineId id, LoanId loanId, BookId bookId, string bookTitle, DateOnly dueOn)
    {
        Id = id;
        LoanId = loanId;
        BookId = bookId;
        BookTitle = bookTitle;
        DueOn = dueOn;
    }

    public LoanLineId Id { get; private set; }
    public LoanId LoanId { get; private set; }
    public BookId BookId { get; private set; }
    public string BookTitle { get; private set; }
    public DateOnly DueOn { get; private set; }
    public DateOnly? ReturnedOn { get; private set; }

    public static LoanLine Create(LoanId loanId, BookId bookId, string bookTitle, DateOnly dueOn) =>
        new(LoanLineId.New(), loanId, bookId, bookTitle, dueOn);
}

public readonly record struct LoanLineId(Guid Value)
{
    public static LoanLineId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
