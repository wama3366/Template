using FluentValidation;
using LibraryTemplate.Application.Dependencies.Persistence;
using LibraryTemplate.Domain.Dependencies.Persistence;

namespace LibraryTemplate.Application.Services.Loans;

#region Requests

public sealed record StartLoanAppRequest
{
    public IReadOnlyCollection<Guid> BookIds { get; init; } = [];
}

#endregion Requests

#region Responses

public sealed record StartLoanAppResponse(
    Guid LoanId,
    string LoanNumber,
    DateOnly BorrowedOn,
    IReadOnlyCollection<LoanLineAppResponse> Lines);

public sealed record LoanLineAppResponse(
    Guid BookId,
    string BookTitle,
    DateOnly DueOn);

public sealed record BorrowedLoanAppResponse(
    Guid LoanId,
    string LoanNumber,
    Guid BranchId,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    DateOnly DueOn)
{
    public static BorrowedLoanAppResponse FromProjection(BorrowedLoanProjection projection) =>
        new(
            projection.LoanId.Value,
            projection.LoanNumber,
            projection.BranchId.Value,
            projection.BorrowerId.Value,
            projection.BorrowerName,
            projection.BorrowerEmail,
            projection.BookTitle,
            projection.DueOn);
}

#endregion Responses

#region Request Validators

public sealed class StartLoanAppRequestValidator : AbstractValidator<StartLoanAppRequest>
{
    public StartLoanAppRequestValidator()
    {
        RuleFor(request => request.BookIds)
            .NotEmpty()
            .WithMessage("At least one book is required.")
            .Must(bookIds => bookIds.All(bookId => bookId != Guid.Empty))
            .WithMessage("Book ids must be valid.")
            .Must(bookIds => bookIds.Distinct().Count() == bookIds.Count)
            .WithMessage("A book can be included only once.")
            .Must(bookIds => bookIds.Count <= 10)
            .WithMessage("A single loan can include at most 10 books.");
    }
}

#endregion Request Validators
