using LibraryTemplate.Api.Contracts;
using LibraryTemplate.Application.Services.Branches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryTemplate.Api.Controllers;

[ApiController]
[Authorize]
public sealed class BranchesController(IBranchAppService branchAppService) : ControllerBase
{
    [HttpPost("branches/{branchId:guid}/delete")]
    public async Task<ActionResult<MarkBranchDeletedApiResponse>> MarkDeleted(
        Guid branchId,
        MarkBranchDeletedApiRequest apiRequest)
    {
        var appResponse = await branchAppService.MarkDeletedAsync(
            branchId,
            new MarkBranchDeletedAppRequest { Reason = apiRequest.Reason });

        return Ok(MarkBranchDeletedApiResponse.FromAppResponse(appResponse));
    }
}
