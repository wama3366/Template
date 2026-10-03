using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class BookRepository(LibraryDbContext dbContext) : IBookRepository
{
    public Task<Book?> FindByIdAsync(BookId bookId) =>
        dbContext.Books.SingleOrDefaultAsync(book => book.Id == bookId);

    public async Task<IReadOnlyCollection<Book>> FindByIdsAsync(IReadOnlyCollection<BookId> bookIds) =>
        await dbContext.Books
            .Where(book => bookIds.Contains(book.Id))
            .ToArrayAsync();

    public Task<bool> ExistsWithIsbnAsync(Isbn isbn) =>
        dbContext.Books.AnyAsync(book => book.Isbn == isbn);

    public void AddWithAuthor(Book book, AuthorId authorId)
    {
        dbContext.Books.Add(book);
        dbContext.BookAuthors.Add(BookAuthorRow.Create(book.Id, authorId));
    }
}
