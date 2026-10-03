using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class AuthorRepository(LibraryDbContext dbContext) : IAuthorRepository
{
    public async Task<Author?> FindByNameAsync(string name)
    {
        var normalizedName = Author.NormalizeName(name);
        var trackedAuthor = dbContext.Authors.Local.SingleOrDefault(author => author.Name == normalizedName);

        if (trackedAuthor is not null)
        {
            return trackedAuthor;
        }

        return await dbContext.Authors
            .AsNoTracking()
            .SingleOrDefaultAsync(author => author.Name == normalizedName);
    }

    public void Add(Author author) => dbContext.Authors.Add(author);
}
