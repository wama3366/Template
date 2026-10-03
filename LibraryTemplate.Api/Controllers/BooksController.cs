using LibraryTemplate.Api.Contracts;
using LibraryTemplate.Application.Services.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryTemplate.Api.Controllers;

[ApiController]
[Authorize]
public sealed class BooksController(IBookAppService bookAppService) : ControllerBase
{
    [HttpPost("books")]
    public async Task<ActionResult<AddBookApiResponse>> AddBook(AddBookApiRequest apiRequest)
    {
        var appResponse = await bookAppService.AddBookAsync(
            new AddBookAppRequest
            {
                Title = apiRequest.Title,
                AuthorName = apiRequest.AuthorName,
                Isbn = apiRequest.Isbn,
                AgeRestriction = apiRequest.AgeRestriction
            });

        return Created($"/books/{appResponse.BookId}", AddBookApiResponse.FromAppResponse(appResponse));
    }

    [HttpPost("books/{bookId:guid}/block")]
    public async Task<ActionResult<BlockBookApiResponse>> BlockBook(
        Guid bookId,
        BlockBookApiRequest apiRequest)
    {
        var appResponse = await bookAppService.BlockBookAsync(
            bookId,
            new BlockBookAppRequest { Reason = apiRequest.Reason });

        return Ok(BlockBookApiResponse.FromAppResponse(appResponse));
    }
}
