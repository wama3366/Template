using FluentValidation;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application.Services.Loans;

public interface ILoanAppService
{
    Task<StartLoanAppResponse> StartLoanAsync(Guid branchId, StartLoanAppRequest request);
    Task<IReadOnlyCollection<BorrowedLoanAppResponse>> GetBorrowedForBranchAsync(Guid branchId);
    Task<IReadOnlyCollection<BorrowedLoanAppResponse>> GetAllBorrowedAsync();
}

public sealed class LoanAppService(
    IValidator<StartLoanAppRequest> startLoanValidator,
    ICurrentUserProvider currentUserProvider,
    IUserRepository userRepository,
    IUserBranchRepository userBranchRepository,
    IBorrowerProfileRepository borrowerProfileRepository,
    IBookRepository bookRepository,
    ILoanRepository loanRepository,
    ILoanOverviewRepository loanOverviewRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<LoanAppService> logger) :
    ApplicationServiceBase<LoanAppService>(currentUserProvider, userRepository, unitOfWork, clock, logger),
    ILoanAppService
{
    public async Task<StartLoanAppResponse> StartLoanAsync(
        Guid branchId,
        StartLoanAppRequest request)
    {
        var id = new BranchId(branchId);

        // Request validation
        await startLoanValidator.ValidateAndThrowAsync(request);

        // Role-based authorization
        var borrower = await EnsureCurrentUserIsBorrower();

        // Sanity checks
        if (!await userBranchRepository.UserHasBranchAsync(borrower.Id, id))
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        var borrowerCategory = await borrowerProfileRepository.FindCategoryByUserIdAsync(borrower.Id)
            ?? throw new NotFoundException("Borrower profile was not found.");

        var requestedBookIds = request.BookIds.Distinct().Select(bookId => new BookId(bookId)).ToArray();
        var foundBooks = await bookRepository.FindByIdsAsync(requestedBookIds);

        if (foundBooks.Count != requestedBookIds.Length)
        {
            throw new NotFoundException("One or more requested books could not be found.");
        }

        // Domain actions
        var loan = Loan.Start(id, borrower.Id, borrowerCategory, foundBooks, Clock.Today);

        loanRepository.Add(loan);

        // Transaction commit
        await UnitOfWork.SaveChangesAsync();

        // After-save side effects
        Logger.LogInformation(
            "Started loan {LoanId} for borrower {BorrowerId} in branch {BranchId}",
            loan.Id,
            borrower.Id,
            branchId);

        return new StartLoanAppResponse(
            loan.Id.Value,
            loan.LoanNumber.Value,
            loan.BorrowedOn,
            loan.Lines
                .Select(line => new LoanLineAppResponse(line.BookId.Value, line.BookTitle, line.DueOn))
                .ToArray());
    }

    public async Task<IReadOnlyCollection<BorrowedLoanAppResponse>> GetBorrowedForBranchAsync(Guid branchId)
    {
        var id = new BranchId(branchId);

        // Role-based authorization
        var user = await EnsureCurrentUserHasRole(UserRole.SystemAdmin, UserRole.LibraryAdmin);

        // Sanity checks
        var canViewBranchLoans = user.HasRole(UserRole.SystemAdmin) || await userBranchRepository.UserHasBranchAsync(user.Id, id);
        if (!canViewBranchLoans)
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        // Domain actions
        var borrowed = await loanOverviewRepository.ListBorrowedForBranchAsync(id);
        return borrowed.Select(BorrowedLoanAppResponse.FromProjection).ToArray();
    }

    public async Task<IReadOnlyCollection<BorrowedLoanAppResponse>> GetAllBorrowedAsync()
    {
        // Role-based authorization
        await EnsureCurrentUserIsSystemAdmin();

        // Domain actions
        var borrowed = await loanOverviewRepository.ListBorrowedAsync();
        return borrowed.Select(BorrowedLoanAppResponse.FromProjection).ToArray();
    }
}
