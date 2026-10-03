using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application;

public abstract class ApplicationServiceBase<TService>(
    ICurrentUserProvider currentUserProvider,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<TService> logger)
{
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IClock Clock { get; } = clock;
    protected ILogger<TService> Logger { get; } = logger;

    private Task<User?> CurrentUser =>
        field ??= userRepository.FindByExternalUserIdAsync(currentUserProvider.ExternalUserId);

    protected async Task<User> EnsureCurrentUserIsBorrower()
    {
        var user = await CurrentUser;
        if (user is null || !user.HasRole(UserRole.Borrower))
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        return user;
    }

    protected async Task<User> EnsureCurrentUserIsLibraryAdmin()
    {
        var user = await CurrentUser;
        if (user is null || !user.HasRole(UserRole.LibraryAdmin))
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        return user;
    }

    protected async Task<User> EnsureCurrentUserIsSystemAdmin()
    {
        var user = await CurrentUser;
        if (user is null || !user.HasRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        return user;
    }

    protected async Task<User> EnsureCurrentUserHasRole(params UserRole[] roles)
    {
        var user = await CurrentUser;
        if (user is null)
        {
            throw new ForbiddenException("The authenticated user does not have permission for this action.");
        }

        foreach (var role in roles)
        {
            if (user.HasRole(role))
            {
                return user;
            }
        }

        throw new ForbiddenException("The authenticated user does not have permission for this action.");
    }
}
