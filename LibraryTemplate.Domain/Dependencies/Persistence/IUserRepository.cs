using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IUserRepository
{
    Task<User?> FindByExternalUserIdAsync(string externalUserId);
    Task<User?> FindByIdAsync(UserId userId);
}
