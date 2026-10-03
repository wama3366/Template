using LibraryTemplate.Domain.Aggregates.Branches;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IBranchRepository
{
    Task<Branch?> FindByIdAsync(BranchId branchId);
}
