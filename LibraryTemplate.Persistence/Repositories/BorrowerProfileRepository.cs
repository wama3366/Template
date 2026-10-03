using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class BorrowerProfileRepository(LibraryDbContext dbContext) : IBorrowerProfileRepository
{
    public async Task<BorrowerCategory?> FindCategoryByUserIdAsync(UserId userId) =>
        await dbContext.BorrowerProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => (BorrowerCategory?)profile.Category)
            .SingleOrDefaultAsync();
}
