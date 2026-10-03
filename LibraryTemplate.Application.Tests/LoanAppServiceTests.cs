using FluentAssertions;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Application.Services.Loans;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LibraryTemplate.Application.Tests;

public sealed class LoanAppServiceTests
{
    [Fact]
    public async Task Borrower_can_start_allowed_loan_for_assigned_branch()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var userId = UserId.New();
        var book = await CreateBookAsync("The Clean Shelf", "9780000000201", AgeRestriction.General);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var loans = Substitute.For<ILoanRepository>();

        var service = CreateService(
            userId,
            UserRole.Borrower,
            branchId,
            [book],
            unitOfWork,
            loans);

        // Act
        var response = await service.StartLoanAsync(
            branchId,
            new StartLoanAppRequest { BookIds = [book.Id.Value] });

        // Assert
        response.LoanId.Should().NotBeEmpty();
        response.Lines.Should().ContainSingle(line => line.BookId == book.Id.Value);
        loans.Received(1).Add(Arg.Any<Loan>());
        await unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Role_failure_stops_business_work()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var otherBranchId = Guid.CreateVersion7();
        var userId = UserId.New();
        var book = await CreateBookAsync("The Clean Shelf", "9780000000202", AgeRestriction.General);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var loans = Substitute.For<ILoanRepository>();
        var books = Substitute.For<IBookRepository>();

        var service = CreateService(
            userId,
            UserRole.Borrower,
            otherBranchId,
            [book],
            unitOfWork,
            loans,
            books: books);

        // Act
        var act = () => service.StartLoanAsync(
            branchId,
            new StartLoanAppRequest { BookIds = [book.Id.Value] });

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        await books.DidNotReceive().FindByIdsAsync(Arg.Any<IReadOnlyCollection<BookId>>());
        loans.DidNotReceive().Add(Arg.Any<Loan>());
        await unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task System_admin_can_view_all_borrowed_loans()
    {
        // Arrange
        var userId = UserId.New();
        var branchId = Guid.CreateVersion7();
        var loanOverviewRepository = Substitute.For<ILoanOverviewRepository>();
        var borrowedLoan = new BorrowedLoanProjection(
            LoanId.New(),
            "LN-1",
            new BranchId(branchId),
            userId,
            "Adult Borrower",
            "adult@example.test",
            "The Clean Shelf",
            new DateOnly(2026, 7, 1));

        loanOverviewRepository.ListBorrowedAsync()
            .Returns([borrowedLoan]);

        var service = CreateService(
            userId,
            UserRole.SystemAdmin,
            branchId: null,
            [],
            Substitute.For<IUnitOfWork>(),
            Substitute.For<ILoanRepository>(),
            loanOverviewRepository: loanOverviewRepository);

        // Act
        var response = await service.GetAllBorrowedAsync();

        // Assert
        response.Should().ContainSingle(loan => loan.LoanId == borrowedLoan.LoanId.Value);
        await loanOverviewRepository.Received(1).ListBorrowedAsync();
    }

    [Fact]
    public async Task Library_admin_can_view_borrowed_loans_for_assigned_branch()
    {
        // Arrange
        var userId = UserId.New();
        var branchId = Guid.CreateVersion7();
        var loanOverviewRepository = Substitute.For<ILoanOverviewRepository>();

        loanOverviewRepository.ListBorrowedForBranchAsync(new BranchId(branchId))
            .Returns([]);

        var service = CreateService(
            userId,
            UserRole.LibraryAdmin,
            branchId,
            [],
            Substitute.For<IUnitOfWork>(),
            Substitute.For<ILoanRepository>(),
            loanOverviewRepository: loanOverviewRepository);

        // Act
        await service.GetBorrowedForBranchAsync(branchId);

        // Assert
        await loanOverviewRepository.Received(1).ListBorrowedForBranchAsync(new BranchId(branchId));
    }

    private static LoanAppService CreateService(
        UserId userId,
        UserRole role,
        Guid? branchId,
        IReadOnlyCollection<Book> booksToReturn,
        IUnitOfWork unitOfWork,
        ILoanRepository loans,
        IBookRepository? books = null,
        ILoanOverviewRepository? loanOverviewRepository = null)
    {
        const string externalUserId = "external-user-1";

        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.ExternalUserId.Returns(externalUserId);

        var users = Substitute.For<IUserRepository>();
        users.FindByExternalUserIdAsync(externalUserId)
            .Returns(User.Load(
                userId,
                "Adult Borrower",
                "adult@example.test",
                [role]));

        var userBranches = Substitute.For<IUserBranchRepository>();
        if (branchId is not null)
        {
            userBranches.UserHasBranchAsync(userId, new BranchId(branchId.Value))
                .Returns(true);
        }

        var borrowerProfiles = Substitute.For<IBorrowerProfileRepository>();
        borrowerProfiles.FindCategoryByUserIdAsync(userId)
            .Returns(BorrowerCategory.Adult);

        books ??= Substitute.For<IBookRepository>();
        books.FindByIdsAsync(Arg.Any<IReadOnlyCollection<BookId>>())
            .Returns(booksToReturn);

        var clock = Substitute.For<IClock>();
        clock.Today.Returns(new DateOnly(2026, 6, 17));

        return new LoanAppService(
            new StartLoanAppRequestValidator(),
            currentUserProvider,
            users,
            userBranches,
            borrowerProfiles,
            books,
            loans,
            loanOverviewRepository ?? Substitute.For<ILoanOverviewRepository>(),
            unitOfWork,
            clock,
            NullLogger<LoanAppService>.Instance);
    }

    private static async Task<Book> CreateBookAsync(
        string title,
        string isbn,
        AgeRestriction ageRestriction)
    {
        var books = Substitute.For<IBookRepository>();
        var authors = Substitute.For<IAuthorRepository>();
        books.ExistsWithIsbnAsync(Isbn.Create(isbn)).Returns(false);
        authors.FindByNameAsync("Template Author").Returns((Author?)null);

        var libraryCatalogDomainService = new LibraryCatalogDomainService(books, authors);
        var (book, _) = await libraryCatalogDomainService.AddBookAsync(
            title,
            "Template Author",
            Isbn.Create(isbn),
            ageRestriction);

        return book;
    }
}
