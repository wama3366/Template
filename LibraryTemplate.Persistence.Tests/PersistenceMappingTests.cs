using FluentAssertions;
using LibraryTemplate.Application.Events;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Events;
using LibraryTemplate.Domain.Services;
using LibraryTemplate.Persistence;
using LibraryTemplate.Persistence.Rows;
using LibraryTemplate.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LibraryTemplate.Persistence.Tests;

[Collection(PostgresCollection.Name)]
public sealed class PersistenceMappingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_do_not_load_seed_data()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        await PersistenceTestDataBuilder.ResetAsync(dbContext);

        // Act
        var modelSeedData = dbContext.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .SelectMany(entity => entity.GetSeedData())
            .ToArray();
        var authorEntity = dbContext.Model.FindEntityType(typeof(Author));
        var bookAuthorEntity = dbContext.Model.FindEntityType(typeof(BookAuthorRow));
        var roleAssignmentEntity = dbContext.Model.FindEntityType(typeof(RoleAssignmentRow));
        var userBranchEntity = dbContext.Model.FindEntityType(typeof(UserBranchRow));
        var rowCounts = new[]
        {
            await dbContext.Roles.CountAsync(),
            await dbContext.Users.CountAsync(),
            await dbContext.Branches.CountAsync(),
            await dbContext.Books.CountAsync(),
            await dbContext.Authors.CountAsync()
        };
        var modelTypeNames = dbContext.Model.GetEntityTypes()
            .Select(entity => entity.ClrType.Name)
            .ToArray();
        var hasUniqueAuthorNameIndex = authorEntity?.GetIndexes()
            .Any(index => index.Properties.Select(property => property.Name).SequenceEqual(["Name"]) && index.IsUnique) == true;

        // Assert
        modelSeedData.Should().BeEmpty();
        rowCounts.Should().AllBeEquivalentTo(0);
        authorEntity.Should().NotBeNull();
        hasUniqueAuthorNameIndex.Should().BeTrue();
        bookAuthorEntity.Should().NotBeNull();
        bookAuthorEntity!.GetTableName().Should().Be("book_authors");
        roleAssignmentEntity.Should().NotBeNull();
        roleAssignmentEntity!.FindProperty("BranchId").Should().BeNull();
        userBranchEntity.Should().NotBeNull();
        modelTypeNames.Should().NotIntersectWith(
            [
                "Permission",
                "RolePermission",
                "BookAuthor",
                "RoleAssignment",
                "UserBranchAssignment",
                "Role"
            ]);
    }

    [Fact]
    public async Task Book_author_many_to_many_mapping_round_trips()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);

        // Act
        var link = await dbContext.BookAuthors
            .SingleAsync(entity => entity.BookId == data.GeneralBookId);
        var author = await dbContext.Authors
            .SingleAsync(entity => entity.Id == link.AuthorId);

        // Assert
        author.Name.Should().StartWith("Template Author");
    }

    [Fact]
    public async Task User_repository_returns_user_with_role_values_from_role_assignments()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var userRepository = new UserRepository(dbContext);

        // Act
        var user = await userRepository.FindByIdAsync(data.SystemAdminUserId);

        // Assert
        user.Should().NotBeNull();
        user!.HasRole(UserRole.SystemAdmin).Should().BeTrue();
        user.Roles.Should().ContainSingle(role => role == UserRole.SystemAdmin);
    }

    [Fact]
    public async Task User_branch_repository_uses_user_branch_rows()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var userBranchRepository = new UserBranchRepository(dbContext);

        // Act
        var assignedBranch = await userBranchRepository.UserHasBranchAsync(data.BranchAdminUserId, data.BranchId);
        var unassignedBranch = await userBranchRepository.UserHasBranchAsync(data.SystemAdminUserId, data.BranchId);

        // Assert
        assignedBranch.Should().BeTrue();
        unassignedBranch.Should().BeFalse();
    }

    [Fact]
    public async Task Book_repository_adds_book_and_checks_isbn_existence()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        await PersistenceTestDataBuilder.ResetAsync(dbContext);
        var isbn = Isbn.Create("9780000000501");
        var bookRepository = new BookRepository(dbContext);
        var authorRepository = new AuthorRepository(dbContext);
        var libraryCatalogDomainService = new LibraryCatalogDomainService(bookRepository, authorRepository);
        var (book, author) = await libraryCatalogDomainService.AddBookAsync(
            "The Clean Shelf",
            "Template Author",
            isbn,
            AgeRestriction.General);

        // Act
        await dbContext.SaveChangesAsync();

        var matchingIsbnExists = await bookRepository.ExistsWithIsbnAsync(isbn);
        var otherIsbnExists = await bookRepository.ExistsWithIsbnAsync(Isbn.Create("9780000000502"));
        var bookAuthorExists = await dbContext.BookAuthors.AnyAsync(link =>
            link.BookId == book.Id
            && link.AuthorId == author.Id);

        // Assert
        matchingIsbnExists.Should().BeTrue();
        otherIsbnExists.Should().BeFalse();
        bookAuthorExists.Should().BeTrue();
    }

    [Fact]
    public async Task Author_repository_uses_domain_name_normalization_for_lookup_and_save()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        await PersistenceTestDataBuilder.ResetAsync(dbContext);
        var authors = new AuthorRepository(dbContext);
        var author = Author.Create("  Template Author  ");

        authors.Add(author);

        // Act
        var foundBeforeSave = await authors.FindByNameAsync("Template Author");
        await dbContext.SaveChangesAsync();
        var foundAfterSave = await authors.FindByNameAsync("  Template Author  ");

        // Assert
        author.Name.Should().Be("Template Author");
        foundBeforeSave.Should().Be(author);
        foundAfterSave.Should().NotBeNull();
        foundAfterSave!.Name.Should().Be("Template Author");
    }

    [Fact]
    public async Task Loans_save_with_lines_and_uuid_v7_ids()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var book = await dbContext.Books.SingleAsync(entity => entity.Id == data.GeneralBookId);
        var loan = Loan.Start(
            data.BranchId,
            data.AdultBorrowerUserId,
            BorrowerCategory.Adult,
            [book],
            new DateOnly(2026, 6, 17));

        var loanRepository = new LoanRepository(dbContext);

        // Act
        loanRepository.Add(loan);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.Loans
            .Include(entity => entity.Lines)
            .SingleAsync(entity => entity.Id == loan.Id);

        // Assert
        saved.Id.ToString().Split('-')[2][0].Should().Be('7');
        saved.Lines.Should().ContainSingle(line => line.BookId == book.Id);
    }

    [Fact]
    public async Task Loan_overview_repository_returns_borrowed_loans_with_borrower_details()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var book = await dbContext.Books.SingleAsync(entity => entity.Id == data.GeneralBookId);
        var loan = Loan.Start(
            data.BranchId,
            data.AdultBorrowerUserId,
            BorrowerCategory.Adult,
            [book],
            new DateOnly(2026, 6, 17));

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync();

        var loanOverviewRepository = new LoanOverviewRepository(dbContext);

        // Act
        var borrowed = await loanOverviewRepository.ListBorrowedForBranchAsync(
            data.BranchId);

        // Assert
        borrowed.Should().Contain(item =>
            item.LoanId == loan.Id
            && item.BorrowerEmail == data.AdultBorrowerEmail
            && item.BookTitle == book.Title);
    }

    [Fact]
    public async Task Loan_overview_repository_returns_overdue_loans_with_borrower_details()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var book = await dbContext.Books.SingleAsync(entity => entity.Id == data.GeneralBookId);
        var loan = Loan.Start(
            data.BranchId,
            data.AdultBorrowerUserId,
            BorrowerCategory.Adult,
            [book],
            new DateOnly(2026, 1, 1));

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync();

        var loanOverviewRepository = new LoanOverviewRepository(dbContext);

        // Act
        var overdue = await loanOverviewRepository.ListOverdueAsync(
            new DateOnly(2026, 6, 17),
            take: 10);

        // Assert
        overdue.Should().Contain(item =>
            item.LoanId == loan.Id
            && item.BorrowerEmail == data.AdultBorrowerEmail
            && item.BookTitle == book.Title);
    }

    [Fact]
    public async Task Branch_repository_loads_and_persists_deleted_state()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        await PersistenceTestDataBuilder.ResetAsync(dbContext);
        var branchToDelete = Branch.Create("Temporary Branch");
        dbContext.Branches.Add(branchToDelete);
        await dbContext.SaveChangesAsync();

        var branchRepository = new BranchRepository(dbContext);

        var branch = await branchRepository.FindByIdAsync(branchToDelete.Id)
            ?? throw new InvalidOperationException("Branch was not found.");
        var deletedAt = new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);

        // Act
        branch.MarkDeleted("Closed for renovation.", deletedAt);
        await dbContext.SaveChangesAsync();

        var saved = await dbContext.Branches.SingleAsync(entity => entity.Id == branchToDelete.Id);

        // Assert
        saved.IsDeleted.Should().BeTrue();
        saved.DeletedAt.Should().Be(deletedAt);
        saved.DeletedReason.Should().Be("Closed for renovation.");
    }

    [Fact]
    public async Task Unit_of_work_dispatches_domain_events_after_successful_save()
    {
        // Arrange
        await using var dbContext = fixture.CreateDbContext();
        var data = await ResetAndCreateDataAsync(dbContext);
        var dispatcher = new PersistedLoanCheckingDispatcher(dbContext);
        var unitOfWork = new UnitOfWork(dbContext, dispatcher);
        var book = await dbContext.Books.SingleAsync(entity => entity.Id == data.GeneralBookId);
        var loan = Loan.Start(
            data.BranchId,
            data.AdultBorrowerUserId,
            BorrowerCategory.Adult,
            [book],
            new DateOnly(2026, 6, 17));

        dbContext.Loans.Add(loan);

        // Act
        await unitOfWork.SaveChangesAsync();

        // Assert
        dispatcher.SeenPersistedLoan.Should().BeTrue();
        loan.DomainEvents.Should().BeEmpty();
    }

    private static async Task<PersistenceTestData> ResetAndCreateDataAsync(LibraryDbContext dbContext)
    {
        await PersistenceTestDataBuilder.ResetAsync(dbContext);
        return await PersistenceTestDataBuilder.CreateAsync(dbContext);
    }

    private sealed class PersistedLoanCheckingDispatcher(LibraryDbContext dbContext) : IDomainEventDispatcher
    {
        public bool SeenPersistedLoan { get; private set; }

        public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            var loanStarted = domainEvents.OfType<LoanStartedEvent>().Single();
            SeenPersistedLoan = await dbContext.Loans.AnyAsync(
                loan => loan.Id == loanStarted.LoanId);
        }
    }
}
