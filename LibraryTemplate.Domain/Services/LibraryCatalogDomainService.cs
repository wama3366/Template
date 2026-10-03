using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;

namespace LibraryTemplate.Domain.Services;

public sealed class LibraryCatalogDomainService(
    IBookRepository bookRepository,
    IAuthorRepository authorRepository)
{
    public async Task<(Book Book, Author Author)> AddBookAsync(
        string title,
        string authorName,
        Isbn isbn,
        AgeRestriction ageRestriction)
    {
        // Business logic that spans more than one aggregate (multiple books) goes in a Domain Service.
        if (await bookRepository.ExistsWithIsbnAsync(isbn))
        {
            throw new DomainException("Book.IsbnAlreadyExists", $"A book with ISBN '{isbn}' already exists.");
        }

        // Business logic that spans across aggregates (books and authors) goes in a Domain Service.
        var author = await authorRepository.FindByNameAsync(authorName);
        if (author is null)
        {
            author = Author.Create(authorName);
            authorRepository.Add(author);
        }

        var book = Book.Create(title, isbn, ageRestriction);
        bookRepository.AddWithAuthor(book, author.Id);

        return (book, author);
    }
}
