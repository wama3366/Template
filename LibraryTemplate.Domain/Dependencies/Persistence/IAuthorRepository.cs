using LibraryTemplate.Domain.Aggregates.Books;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IAuthorRepository
{
    Task<Author?> FindByNameAsync(string name);
    void Add(Author author);
}
