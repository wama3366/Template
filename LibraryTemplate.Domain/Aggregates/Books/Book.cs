namespace LibraryTemplate.Domain.Aggregates.Books;

public sealed class Book : AggregateRoot<BookId>
{
    private Book()
    {
        Title = string.Empty;
        Isbn = Isbn.Create("0000000000000");
    }

    private Book(BookId id, string title, Isbn isbn, AgeRestriction ageRestriction) : base(id)
    {
        Title = title.Trim();
        Isbn = isbn;
        AgeRestriction = ageRestriction;
    }

    public string Title { get; private set; }
    public Isbn Isbn { get; private set; }
    public AgeRestriction AgeRestriction { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? BlockReason { get; private set; }

    internal static Book Create(string title, Isbn isbn, AgeRestriction ageRestriction)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Book.TitleRequired", "Book title is required.");
        }

        return new Book(BookId.New(), title, isbn, ageRestriction);
    }

    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Book.BlockReasonRequired", "A reason is required when blocking a book.");
        }

        IsBlocked = true;
        BlockReason = reason.Trim();
    }
}

public readonly record struct BookId(Guid Value)
{
    public static BookId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
