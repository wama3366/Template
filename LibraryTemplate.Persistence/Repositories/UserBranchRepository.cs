using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class UserBranchRepository(LibraryDbContext dbContext) : IUserBranchRepository
{
    public Task<bool> UserHasBranchAsync(UserId userId, BranchId branchId) =>
        dbContext.UserBranches
            .AsNoTracking()
            .AnyAsync(assignment => assignment.UserId == userId && assignment.BranchId == branchId);
}
