using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class UserRepository(LibraryDbContext dbContext) : IUserRepository
{
    public async Task<User?> FindByExternalUserIdAsync(string externalUserId)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(entity => entity.ExternalUserId == externalUserId)
            .Select(entity => new
            {
                entity.Id,
                entity.ExternalUserId,
                entity.DisplayName,
                entity.Email
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.RoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == user.Id)
            .Select(assignment => assignment.Role)
            .ToArrayAsync();

        return User.Load(user.Id, user.ExternalUserId, user.DisplayName, user.Email, roles);
    }

    public async Task<User?> FindByIdAsync(UserId userId)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(entity => entity.Id == userId)
            .Select(entity => new
            {
                entity.Id,
                entity.ExternalUserId,
                entity.DisplayName,
                entity.Email
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.RoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == user.Id)
            .Select(assignment => assignment.Role)
            .ToArrayAsync();

        return User.Load(user.Id, user.ExternalUserId, user.DisplayName, user.Email, roles);
    }
}
