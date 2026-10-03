using FluentValidation;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LibraryTemplate.Application.Services.Books;

public interface IBookAppService
{
    Task<AddBookAppResponse> AddBookAsync(AddBookAppRequest request);
    Task<BlockBookAppResponse> BlockBookAsync(Guid bookId, BlockBookAppRequest request);
}

public sealed class BookAppService(
    IValidator<AddBookAppRequest> addBookValidator,
    IValidator<BlockBookAppRequest> blockBookValidator,
    ICurrentUserProvider currentUserProvider,
    IUserRepository userRepository,
    IBookRepository bookRepository,
    LibraryCatalogDomainService libraryCatalogDomainService,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<BookAppService> logger) :
    ApplicationServiceBase<BookAppService>(currentUserProvider, userRepository, unitOfWork, clock, logger),
    IBookAppService
{
    public async Task<AddBookAppResponse> AddBookAsync(AddBookAppRequest request)
    {
        // Request validation
        await addBookValidator.ValidateAndThrowAsync(request);

        // Role-based authorization
        await EnsureCurrentUserIsSystemAdmin();

        // Domain actions
        var (book, author) = await libraryCatalogDomainService.AddBookAsync(
            request.Title,
            request.AuthorName,
            Isbn.Create(request.Isbn),
            request.AgeRestriction);

        // Transaction commit
        await UnitOfWork.SaveChangesAsync();

        // After-save side effects
        Logger.LogInformation("Added book {BookId}", book.Id);

        return new AddBookAppResponse(
            book.Id.Value,
            book.Title,
            author.Id.Value,
            author.Name,
            book.Isbn.Value,
            book.AgeRestriction);
    }

    public async Task<BlockBookAppResponse> BlockBookAsync(
        Guid bookId,
        BlockBookAppRequest request)
    {
        var id = new BookId(bookId);

        // Request validation
        await blockBookValidator.ValidateAndThrowAsync(request);

        // Role-based authorization
        await EnsureCurrentUserIsSystemAdmin();

        // Sanity checks
        var book = await bookRepository.FindByIdAsync(id)
            ?? throw new NotFoundException("Book was not found.");

        // Domain actions
        book.Block(request.Reason);

        // Transaction commit
        await UnitOfWork.SaveChangesAsync();

        // After-save side effects
        Logger.LogInformation("Blocked book {BookId}", bookId);

        return new BlockBookAppResponse(book.Id.Value, book.IsBlocked, book.BlockReason);
    }
}
