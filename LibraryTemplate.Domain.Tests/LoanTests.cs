using FluentAssertions;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;

namespace LibraryTemplate.Domain.Tests;

public sealed class LoanTests
{
    [Fact]
    public async Task Child_cannot_borrow_adult_only_book()
    {
        // Arrange
        var book = await CreateBookAsync("Restricted Stacks", "9780000000101", AgeRestriction.AdultOnly);

        // Act
        var act = () => Loan.Start(
            BranchId.New(),
            UserId.New(),
            BorrowerCategory.Child,
            [book],
            new DateOnly(2026, 6, 17));

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Which.Code.Should()
            .Be("Loan.AgeRestrictedBook");
    }

    [Fact]
    public async Task Blocked_book_cannot_be_borrowed()
    {
        // Arrange
        var book = await CreateBookAsync("The Clean Shelf", "9780000000102", AgeRestriction.General);
        book.Block("Damaged copy.");

        // Act
        var act = () => Loan.Start(
            BranchId.New(),
            UserId.New(),
            BorrowerCategory.Adult,
            [book],
            new DateOnly(2026, 6, 17));

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Which.Code.Should()
            .Be("Loan.BookBlocked");
    }

    [Fact]
    public async Task Starting_a_loan_raises_loan_started()
    {
        // Arrange
        var book = await CreateBookAsync("The Clean Shelf", "9780000000103", AgeRestriction.General);
        var borrowerId = UserId.New();
        var branchId = BranchId.New();

        // Act
        var loan = Loan.Start(branchId, borrowerId, BorrowerCategory.Adult, [book], new DateOnly(2026, 6, 17));

        // Assert
        var domainEvent = loan.DomainEvents.Should().ContainSingle().Subject.Should().BeOfType<LoanStartedEvent>().Subject;
        domainEvent.LoanId.Should().Be(loan.Id);
        domainEvent.BranchId.Should().Be(branchId);
        domainEvent.BorrowerId.Should().Be(borrowerId);
        domainEvent.BookIds.Should().ContainSingle().Which.Should().Be(book.Id);
    }

    private static async Task<Book> CreateBookAsync(
        string title,
        string isbn,
        AgeRestriction ageRestriction)
    {
        var repository = new BookRepositoryStub();
        var authorRepository = new AuthorRepositoryStub();
        var libraryCatalogDomainService = new LibraryCatalogDomainService(repository, authorRepository);
        var (book, _) = await libraryCatalogDomainService.AddBookAsync(
            title,
            "Template Author",
            Isbn.Create(isbn),
            ageRestriction);

        return book;
    }

    private sealed class BookRepositoryStub : IBookRepository
    {
        public Task<Book?> FindByIdAsync(BookId bookId) => Task.FromResult<Book?>(null);

        public Task<IReadOnlyCollection<Book>> FindByIdsAsync(IReadOnlyCollection<BookId> bookIds) =>
            Task.FromResult<IReadOnlyCollection<Book>>([]);

        public Task<bool> ExistsWithIsbnAsync(Isbn isbn) => Task.FromResult(false);

        public void AddWithAuthor(Book book, AuthorId authorId)
        {
        }
    }

    private sealed class AuthorRepositoryStub : IAuthorRepository
    {
        public Task<Author?> FindByNameAsync(string name) => Task.FromResult<Author?>(null);

        public void Add(Author author)
        {
        }
    }
}
