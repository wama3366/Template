using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryTemplate.Api.Contracts;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;
using LibraryTemplate.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryTemplate.Api.IntegrationTests;

public sealed class ApiFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Anonymous_request_is_unauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/loans/borrowed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Adult_borrower_can_start_allowed_loan()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.AdultBorrowerSubject);
        var bookId = await CreateBookAsync(AgeRestriction.General);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{data.BranchId}/loans",
            new StartLoanApiRequest { BookIds = [bookId] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartLoanApiResponse>();
        body.Should().NotBeNull();
        body!.Lines.Should().ContainSingle(line => line.BookId == bookId);
    }

    [Fact]
    public async Task Child_cannot_borrow_adult_only_book()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.ChildBorrowerSubject);
        var bookId = await CreateBookAsync(AgeRestriction.AdultOnly);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{data.BranchId}/loans",
            new StartLoanApiRequest { BookIds = [bookId] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem.Should().ContainKey("code");
    }

    [Fact]
    public async Task Branch_admin_can_view_branch_borrowed_books()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var bookId = await CreateBookAsync(AgeRestriction.General);
        var borrower = factory.CreateClient();
        borrower.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.AdultBorrowerSubject);
        await borrower.PostAsJsonAsync(
            $"/branches/{data.BranchId}/loans",
            new StartLoanApiRequest { BookIds = [bookId] });

        var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.BranchAdminSubject);

        // Act
        var response = await admin.GetAsync($"/branches/{data.BranchId}/loans/borrowed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var borrowed = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<BorrowedLoanApiResponse>>();
        borrowed.Should().NotBeNull();
        borrowed.Should().Contain(loan => loan.BorrowerEmail == data.AdultBorrowerEmail);
    }

    [Fact]
    public async Task Branch_admin_cannot_view_all_borrowed_books()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.BranchAdminSubject);

        // Act
        var response = await client.GetAsync("/loans/borrowed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task System_admin_can_block_book()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);
        var bookId = await CreateBookAsync(AgeRestriction.General);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/books/{bookId}/block",
            new BlockBookApiRequest { Reason = "Missing pages." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BlockBookApiResponse>();
        body.Should().NotBeNull();
        body!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task System_admin_can_add_book()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            "/books",
            new AddBookApiRequest
            {
                Title = "The New Shelf",
                AuthorName = "Template Author",
                Isbn = "9780000000601",
                AgeRestriction = AgeRestriction.General
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AddBookApiResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("The New Shelf");
        body.AuthorName.Should().Be("Template Author");
        body.Isbn.Should().Be("9780000000601");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var author = await dbContext.Authors.SingleAsync(entity => entity.Id == new AuthorId(body.AuthorId));
        var bookAuthorExists = await dbContext.BookAuthors.AnyAsync(entity =>
            entity.BookId == new BookId(body.BookId)
            && entity.AuthorId == author.Id);

        author.Name.Should().Be("Template Author");
        bookAuthorExists.Should().BeTrue();
    }

    [Fact]
    public async Task Adding_duplicate_isbn_returns_conflict_problem()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);
        var request = new AddBookApiRequest
        {
            Title = "The New Shelf",
            AuthorName = "Template Author",
            Isbn = "9780000000602",
            AgeRestriction = AgeRestriction.General
        };

        await client.PostAsJsonAsync("/books", request);

        // Act
        var response = await client.PostAsJsonAsync("/books", request with { Title = "Duplicate Shelf" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem.Should().ContainKey("code");
    }

    [Fact]
    public async Task Branch_admin_cannot_add_book()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.BranchAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            "/books",
            new AddBookApiRequest
            {
                Title = "The New Shelf",
                AuthorName = "Template Author",
                Isbn = "9780000000603",
                AgeRestriction = AgeRestriction.General
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task System_admin_can_mark_branch_deleted()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{data.BranchId}/delete",
            new MarkBranchDeletedApiRequest { Reason = "Closed for renovation." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkBranchDeletedApiResponse>();
        body.Should().NotBeNull();
        body!.IsDeleted.Should().BeTrue();
        body.DeletedReason.Should().Be("Closed for renovation.");
    }

    [Fact]
    public async Task Branch_admin_cannot_mark_branch_deleted()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.BranchAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{data.BranchId}/delete",
            new MarkBranchDeletedApiRequest { Reason = "Closed for renovation." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Marking_missing_branch_deleted_returns_not_found_problem()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{Guid.CreateVersion7()}/delete",
            new MarkBranchDeletedApiRequest { Reason = "Closed for renovation." });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem.Should().ContainKey("title");
    }

    [Fact]
    public async Task Branch_role_assignment_endpoint_no_longer_exists()
    {
        // Arrange
        var data = await factory.ResetDataAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, data.SystemAdminSubject);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/branches/{data.BranchId}/users/{data.AdultBorrowerUserId}/roles",
            new { RoleName = "LibraryAdmin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateBookAsync(AgeRestriction ageRestriction)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var libraryCatalogDomainService = scope.ServiceProvider.GetRequiredService<LibraryCatalogDomainService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var suffix = Random.Shared.Next(100000, 999999);
        var (book, _) = await libraryCatalogDomainService.AddBookAsync(
            $"Integration Book {suffix}",
            $"Integration Author {suffix}",
            Isbn.Create($"9780001{suffix}"),
            ageRestriction);

        await unitOfWork.SaveChangesAsync();
        return book.Id.Value;
    }
}
