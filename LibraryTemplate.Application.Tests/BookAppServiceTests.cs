using FluentAssertions;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Services.Books;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LibraryTemplate.Application.Tests;

public sealed class BookAppServiceTests
{
    [Theory]
    [InlineData("9780000001")]
    [InlineData("9780000000404")]
    [InlineData("978-0-000000-404")]
    public void Add_book_validator_accepts_valid_isbn_formats(string isbn)
    {
        // Arrange
        var validator = new AddBookAppRequestValidator();
        var request = NewRequest(isbn);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("97800000004")]
    [InlineData("97800000004X")]
    public void Add_book_validator_rejects_invalid_isbn_formats(string? isbn)
    {
        // Arrange
        var validator = new AddBookAppRequestValidator();
        var request = NewRequest(isbn);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task System_admin_can_add_book_when_isbn_is_available()
    {
        // Arrange
        var books = Substitute.For<IBookRepository>();
        var authors = Substitute.For<IAuthorRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = CreateService(UserRole.SystemAdmin, books, authors, unitOfWork);
        var request = NewRequest("9780000000401");

        books.ExistsWithIsbnAsync(Isbn.Create(request.Isbn)).Returns(false);
        authors.FindByNameAsync(request.AuthorName).Returns((Author?)null);

        // Act
        var response = await service.AddBookAsync(request);

        // Assert
        response.BookId.Should().NotBeEmpty();
        response.Title.Should().Be(request.Title);
        response.AuthorId.Should().NotBeEmpty();
        response.AuthorName.Should().Be(request.AuthorName);
        response.Isbn.Should().Be(request.Isbn);
        response.AgeRestriction.Should().Be(request.AgeRestriction);
        authors.Received(1).Add(Arg.Is<Author>(author => author.Id.Value == response.AuthorId));
        books.Received(1).AddWithAuthor(
            Arg.Is<Book>(book => book.Id.Value == response.BookId),
            new AuthorId(response.AuthorId));
        await unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Existing_author_is_reused_when_adding_book()
    {
        // Arrange
        var books = Substitute.For<IBookRepository>();
        var authors = Substitute.For<IAuthorRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = CreateService(UserRole.SystemAdmin, books, authors, unitOfWork);
        var request = NewRequest("9780000000405");
        var existingAuthor = Author.Create(request.AuthorName);

        books.ExistsWithIsbnAsync(Isbn.Create(request.Isbn)).Returns(false);
        authors.FindByNameAsync(request.AuthorName).Returns(existingAuthor);

        // Act
        var response = await service.AddBookAsync(request);

        // Assert
        response.AuthorId.Should().Be(existingAuthor.Id.Value);
        response.AuthorName.Should().Be(existingAuthor.Name);
        authors.DidNotReceive().Add(Arg.Any<Author>());
        books.Received(1).AddWithAuthor(
            Arg.Is<Book>(book => book.Id.Value == response.BookId),
            existingAuthor.Id);
        await unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Duplicate_isbn_stops_add_and_save()
    {
        // Arrange
        var books = Substitute.For<IBookRepository>();
        var authors = Substitute.For<IAuthorRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = CreateService(UserRole.SystemAdmin, books, authors, unitOfWork);
        var request = NewRequest("9780000000402");

        books.ExistsWithIsbnAsync(Isbn.Create(request.Isbn)).Returns(true);

        // Act
        var act = () => service.AddBookAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<DomainException>()
            .Where(exception => exception.Code == "Book.IsbnAlreadyExists");
        await authors.DidNotReceive().FindByNameAsync(Arg.Any<string>());
        authors.DidNotReceive().Add(Arg.Any<Author>());
        books.DidNotReceive().AddWithAuthor(Arg.Any<Book>(), Arg.Any<AuthorId>());
        await unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Role_failure_stops_isbn_check_and_add()
    {
        // Arrange
        var books = Substitute.For<IBookRepository>();
        var authors = Substitute.For<IAuthorRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = CreateService(UserRole.LibraryAdmin, books, authors, unitOfWork);
        var request = NewRequest("9780000000403");

        // Act
        var act = () => service.AddBookAsync(request);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        await books.DidNotReceive().ExistsWithIsbnAsync(Arg.Any<Isbn>());
        await authors.DidNotReceive().FindByNameAsync(Arg.Any<string>());
        books.DidNotReceive().AddWithAuthor(Arg.Any<Book>(), Arg.Any<AuthorId>());
        await unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    private static BookAppService CreateService(
        UserRole role,
        IBookRepository books,
        IAuthorRepository authors,
        IUnitOfWork unitOfWork)
    {
        const string externalUserId = "external-user-1";

        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.ExternalUserId.Returns(externalUserId);

        var users = Substitute.For<IUserRepository>();
        users.FindByExternalUserIdAsync(externalUserId)
            .Returns(User.Load(
                UserId.New(),
                "Test User",
                "user@example.test",
                [role]));

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero));

        return new BookAppService(
            new AddBookAppRequestValidator(),
            new BlockBookAppRequestValidator(),
            currentUserProvider,
            users,
            books,
            new LibraryCatalogDomainService(books, authors),
            unitOfWork,
            clock,
            NullLogger<BookAppService>.Instance);
    }

    private static AddBookAppRequest NewRequest(string? isbn) =>
        new()
        {
            Title = "The Clean Shelf",
            AuthorName = "Template Author",
            Isbn = isbn!,
            AgeRestriction = AgeRestriction.General
        };
}
