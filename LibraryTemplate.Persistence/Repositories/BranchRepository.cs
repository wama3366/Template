using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class BranchRepository(LibraryDbContext dbContext) : IBranchRepository
{
    public Task<Branch?> FindByIdAsync(BranchId branchId) =>
        dbContext.Branches.SingleOrDefaultAsync(branch => branch.Id == branchId);
}
