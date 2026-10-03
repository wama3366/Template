using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IUserBranchRepository
{
    Task<bool> UserHasBranchAsync(UserId userId, BranchId branchId);
}
