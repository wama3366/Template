namespace LibraryTemplate.Domain.Aggregates.Loans;

public readonly record struct LoanNumber
{
    private LoanNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LoanNumber New() => new($"LN-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.CreateVersion7().ToString("N")[..6]}");

    public static LoanNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Loan.InvalidLoanNumber", "Loan number is required.");
        }

        return new LoanNumber(value.Trim());
    }

    public override string ToString() => Value;
}
