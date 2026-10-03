using LibraryTemplate.Application.Services.Loans;

namespace LibraryTemplate.Api.Contracts;

public sealed record StartLoanApiRequest
{
    public IReadOnlyCollection<Guid> BookIds { get; init; } = [];
}

public sealed record StartLoanApiResponse(
    Guid LoanId,
    string LoanNumber,
    DateOnly BorrowedOn,
    IReadOnlyCollection<LoanLineApiResponse> Lines)
{
    public static StartLoanApiResponse FromAppResponse(StartLoanAppResponse response) =>
        new(response.LoanId,
            response.LoanNumber,
            response.BorrowedOn,
            response.Lines.Select(LoanLineApiResponse.FromAppResponse).ToArray());
}

public sealed record LoanLineApiResponse(
    Guid BookId,
    string BookTitle,
    DateOnly DueOn)
{
    public static LoanLineApiResponse FromAppResponse(LoanLineAppResponse response) =>
        new(response.BookId, response.BookTitle, response.DueOn);
}

public sealed record BorrowedLoanApiResponse(
    Guid LoanId,
    string LoanNumber,
    Guid BranchId,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    DateOnly DueOn)
{
    public static BorrowedLoanApiResponse FromAppResponse(BorrowedLoanAppResponse response) =>
        new(response.LoanId,
            response.LoanNumber,
            response.BranchId,
            response.BorrowerId,
            response.BorrowerName,
            response.BorrowerEmail,
            response.BookTitle,
            response.DueOn);
}
