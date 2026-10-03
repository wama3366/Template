using LibraryTemplate.Domain.Aggregates.Books;

namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IBookRepository
{
    Task<Book?> FindByIdAsync(BookId bookId);
    Task<IReadOnlyCollection<Book>> FindByIdsAsync(IReadOnlyCollection<BookId> bookIds);
    Task<bool> ExistsWithIsbnAsync(Isbn isbn);
    void AddWithAuthor(Book book, AuthorId authorId);
}
