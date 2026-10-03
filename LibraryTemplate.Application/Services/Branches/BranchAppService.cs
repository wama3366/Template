using FluentValidation;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application.Services.Branches;

public interface IBranchAppService
{
    Task<MarkBranchDeletedAppResponse> MarkDeletedAsync(Guid branchId, MarkBranchDeletedAppRequest request);
}

public sealed class BranchAppService(
    IValidator<MarkBranchDeletedAppRequest> markBranchDeletedValidator,
    ICurrentUserProvider currentUserProvider,
    IUserRepository userRepository,
    IBranchRepository branchRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<BranchAppService> logger) :
    ApplicationServiceBase<BranchAppService>(currentUserProvider, userRepository, unitOfWork, clock, logger),
    IBranchAppService
{
    public async Task<MarkBranchDeletedAppResponse> MarkDeletedAsync(
        Guid branchId,
        MarkBranchDeletedAppRequest request)
    {
        var id = new BranchId(branchId);

        // Request validation
        await markBranchDeletedValidator.ValidateAndThrowAsync(request);

        // Role-based authorization
        await EnsureCurrentUserIsSystemAdmin();

        // Sanity checks
        var branch = await branchRepository.FindByIdAsync(id)
            ?? throw new NotFoundException("Branch was not found.");

        // Domain actions
        branch.MarkDeleted(request.Reason, Clock.UtcNow);

        // Transaction commit
        await UnitOfWork.SaveChangesAsync();

        // After-save side effects
        Logger.LogInformation("Marked branch {BranchId} deleted", branchId);

        return new MarkBranchDeletedAppResponse(
            branch.Id.Value,
            branch.Name,
            branch.IsDeleted,
            branch.DeletedAt,
            branch.DeletedReason);
    }
}
