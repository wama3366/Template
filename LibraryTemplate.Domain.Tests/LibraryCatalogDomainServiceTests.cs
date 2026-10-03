using FluentAssertions;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;

namespace LibraryTemplate.Domain.Tests;

public sealed class LibraryCatalogDomainServiceTests
{
    [Fact]
    public async Task Add_book_creates_book_when_isbn_is_available()
    {
        // Arrange
        var isbn = Isbn.Create("9780000000301");
        var books = new BookRepositoryStub(isbnExists: false);
        var authors = new AuthorRepositoryStub(existingAuthor: null);
        var service = new LibraryCatalogDomainService(books, authors);

        // Act
        var (book, author) = await service.AddBookAsync(
            "The Clean Shelf",
            "Template Author",
            isbn,
            AgeRestriction.General);

        // Assert
        book.Title.Should().Be("The Clean Shelf");
        book.Isbn.Should().Be(isbn);
        book.AgeRestriction.Should().Be(AgeRestriction.General);
        author.Name.Should().Be("Template Author");
        books.CheckedIsbn.Should().Be(isbn);
        books.AddedBook.Should().Be(book);
        authors.AddedAuthor.Should().Be(author);
        books.LinkedAuthorId.Should().Be(author.Id);
    }

    [Fact]
    public async Task Add_book_normalizes_author_name_before_save()
    {
        // Arrange
        var isbn = Isbn.Create("9780000000304");
        var books = new BookRepositoryStub(isbnExists: false);
        var authors = new AuthorRepositoryStub(existingAuthor: null);
        var service = new LibraryCatalogDomainService(books, authors);

        // Act
        var (_, author) = await service.AddBookAsync(
            "The Clean Shelf",
            "  Template Author  ",
            isbn,
            AgeRestriction.General);

        // Assert
        author.Name.Should().Be("Template Author");
        authors.AddedAuthor.Should().Be(author);
    }

    [Fact]
    public async Task Add_book_rejects_duplicate_isbn()
    {
        // Arrange
        var isbn = Isbn.Create("9780000000302");
        var books = new BookRepositoryStub(isbnExists: true);
        var authors = new AuthorRepositoryStub(existingAuthor: null);
        var service = new LibraryCatalogDomainService(books, authors);

        // Act
        var act = () => service.AddBookAsync(
            "The Clean Shelf",
            "Template Author",
            isbn,
            AgeRestriction.General);

        // Assert
        await act.Should()
            .ThrowAsync<DomainException>()
            .Where(exception => exception.Code == "Book.IsbnAlreadyExists");
        books.CheckedIsbn.Should().Be(isbn);
        authors.LookedUpName.Should().BeNull();
        books.AddedBook.Should().BeNull();
    }

    [Fact]
    public async Task Add_book_reuses_existing_author()
    {
        // Arrange
        var isbn = Isbn.Create("9780000000305");
        var existingAuthor = Author.Create("Template Author");
        var books = new BookRepositoryStub(isbnExists: false);
        var authors = new AuthorRepositoryStub(existingAuthor);
        var service = new LibraryCatalogDomainService(books, authors);

        // Act
        var (_, author) = await service.AddBookAsync(
            "The Clean Shelf",
            "Template Author",
            isbn,
            AgeRestriction.General);

        // Assert
        author.Should().Be(existingAuthor);
        authors.AddedAuthor.Should().BeNull();
        books.LinkedAuthorId.Should().Be(existingAuthor.Id);
    }

    private sealed class BookRepositoryStub(bool isbnExists) : IBookRepository
    {
        public Isbn? CheckedIsbn { get; private set; }
        public Book? AddedBook { get; private set; }
        public AuthorId? LinkedAuthorId { get; private set; }

        public Task<Book?> FindByIdAsync(BookId bookId) => Task.FromResult<Book?>(null);

        public Task<IReadOnlyCollection<Book>> FindByIdsAsync(IReadOnlyCollection<BookId> bookIds) =>
            Task.FromResult<IReadOnlyCollection<Book>>([]);

        public Task<bool> ExistsWithIsbnAsync(Isbn isbn)
        {
            CheckedIsbn = isbn;
            return Task.FromResult(isbnExists);
        }

        public void AddWithAuthor(Book book, AuthorId authorId)
        {
            AddedBook = book;
            LinkedAuthorId = authorId;
        }
    }

    private sealed class AuthorRepositoryStub(Author? existingAuthor) : IAuthorRepository
    {
        public string? LookedUpName { get; private set; }
        public Author? AddedAuthor { get; private set; }

        public Task<Author?> FindByNameAsync(string name)
        {
            LookedUpName = name;
            return Task.FromResult(existingAuthor);
        }

        public void Add(Author author)
        {
            AddedAuthor = author;
        }
    }
}
