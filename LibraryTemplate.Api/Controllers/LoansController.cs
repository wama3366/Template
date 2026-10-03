using LibraryTemplate.Api.Contracts;
using LibraryTemplate.Application.Services.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryTemplate.Api.Controllers;

[ApiController]
[Authorize]
public sealed class LoansController(ILoanAppService loanAppService) : ControllerBase
{
    [HttpPost("branches/{branchId:guid}/loans")]
    public async Task<ActionResult<StartLoanApiResponse>> StartLoan(
        Guid branchId,
        StartLoanApiRequest apiRequest)
    {
        var appResponse = await loanAppService.StartLoanAsync(
            branchId,
            new StartLoanAppRequest { BookIds = apiRequest.BookIds });

        var apiResponse = StartLoanApiResponse.FromAppResponse(appResponse);

        return Created($"/loans/{apiResponse.LoanId}", apiResponse);
    }

    [HttpGet("branches/{branchId:guid}/loans/borrowed")]
    public async Task<ActionResult<IReadOnlyCollection<BorrowedLoanApiResponse>>> GetBorrowedForBranch(Guid branchId)
    {
        var appResponse = await loanAppService.GetBorrowedForBranchAsync(branchId);
        return Ok(appResponse.Select(BorrowedLoanApiResponse.FromAppResponse).ToArray());
    }

    [HttpGet("loans/borrowed")]
    public async Task<ActionResult<IReadOnlyCollection<BorrowedLoanApiResponse>>> GetAllBorrowed()
    {
        var appResponse = await loanAppService.GetAllBorrowedAsync();
        return Ok(appResponse.Select(BorrowedLoanApiResponse.FromAppResponse).ToArray());
    }
}
