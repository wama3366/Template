using LibraryTemplate.Application.Services.Branches;

namespace LibraryTemplate.Api.Contracts;

public sealed record MarkBranchDeletedApiRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed record MarkBranchDeletedApiResponse(
    Guid BranchId,
    string Name,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    string? DeletedReason)
{
    public static MarkBranchDeletedApiResponse FromAppResponse(MarkBranchDeletedAppResponse response) =>
        new(response.BranchId,
            response.Name,
            response.IsDeleted,
            response.DeletedAt,
            response.DeletedReason);
}
