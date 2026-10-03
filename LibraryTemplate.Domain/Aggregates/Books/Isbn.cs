namespace LibraryTemplate.Domain.Aggregates.Books;

public readonly record struct Isbn
{
    private Isbn(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return HasValidShape(Normalize(value));
    }

    public static Isbn Create(string value)
    {
        var normalized = Normalize(value);

        if (!HasValidShape(normalized))
        {
            throw new DomainException("Book.InvalidIsbn", "ISBN must contain 10 or 13 digits.");
        }

        return new Isbn(normalized);
    }

    private static string Normalize(string value) =>
        value.Replace("-", string.Empty, StringComparison.Ordinal).Trim();

    private static bool HasValidShape(string normalized) =>
        normalized.Length is 10 or 13 && normalized.All(char.IsDigit);

    public override string ToString() => Value;
}
