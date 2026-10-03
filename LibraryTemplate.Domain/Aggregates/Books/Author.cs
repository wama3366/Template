namespace LibraryTemplate.Domain.Aggregates.Books;

public sealed class Author : AggregateRoot<AuthorId>
{
    private Author()
    {
        Name = string.Empty;
    }

    private Author(AuthorId id, string name) : base(id)
    {
        Name = name.Trim();
    }

    public string Name { get; private set; }

    public static string NormalizeName(string name) => name.Trim();

    public static Author Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Author.NameRequired", "Author name is required.");
        }

        return new Author(AuthorId.New(), NormalizeName(name));
    }
}

public readonly record struct AuthorId(Guid Value)
{
    public static AuthorId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
