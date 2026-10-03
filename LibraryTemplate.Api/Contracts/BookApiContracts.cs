using LibraryTemplate.Application.Services.Books;
using LibraryTemplate.Domain;

namespace LibraryTemplate.Api.Contracts;

public sealed record AddBookApiRequest
{
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public AgeRestriction AgeRestriction { get; init; }
}

public sealed record BlockBookApiRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record AddBookApiResponse(
    Guid BookId,
    string Title,
    Guid AuthorId,
    string AuthorName,
    string Isbn,
    AgeRestriction AgeRestriction)
{
    public static AddBookApiResponse FromAppResponse(AddBookAppResponse response) =>
        new(
            response.BookId,
            response.Title,
            response.AuthorId,
            response.AuthorName,
            response.Isbn,
            response.AgeRestriction);
}

public sealed record BlockBookApiResponse(
    Guid BookId,
    bool IsBlocked,
    string? Reason)
{
    public static BlockBookApiResponse FromAppResponse(BlockBookAppResponse response) =>
        new(response.BookId, response.IsBlocked, response.Reason);
}
