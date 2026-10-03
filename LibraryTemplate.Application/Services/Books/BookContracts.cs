using FluentValidation;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;

namespace LibraryTemplate.Application.Services.Books;

#region Requests

public sealed record AddBookAppRequest
{
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public AgeRestriction AgeRestriction { get; init; }
}

public sealed record BlockBookAppRequest
{
    public string Reason { get; init; } = string.Empty;
}

#endregion Requests

#region Responses

public sealed record AddBookAppResponse(
    Guid BookId,
    string Title,
    Guid AuthorId,
    string AuthorName,
    string Isbn,
    AgeRestriction AgeRestriction);

public sealed record BlockBookAppResponse(
    Guid BookId,
    bool IsBlocked,
    string? Reason);

#endregion Responses

#region Request Validators

public sealed class AddBookAppRequestValidator : AbstractValidator<AddBookAppRequest>
{
    public AddBookAppRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.AuthorName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Isbn)
            .NotEmpty()
            .Must(Isbn.IsValid)
            .WithMessage("ISBN must contain 10 or 13 digits.");

        RuleFor(request => request.AgeRestriction)
            .IsInEnum();
    }
}

public sealed class BlockBookAppRequestValidator : AbstractValidator<BlockBookAppRequest>
{
    public BlockBookAppRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

#endregion Request Validators
