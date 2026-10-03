using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IBorrowerProfileRepository
{
    Task<BorrowerCategory?> FindCategoryByUserIdAsync(UserId userId);
}
