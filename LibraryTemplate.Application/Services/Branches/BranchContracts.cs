using FluentValidation;

namespace LibraryTemplate.Application.Services.Branches;

#region Requests

public sealed record MarkBranchDeletedAppRequest
{
    public string Reason { get; init; } = string.Empty;
}

#endregion Requests

#region Responses

public sealed record MarkBranchDeletedAppResponse(
    Guid BranchId,
    string Name,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    string? DeletedReason);

#endregion Responses

#region Request Validators

public sealed class MarkBranchDeletedAppRequestValidator : AbstractValidator<MarkBranchDeletedAppRequest>
{
    public MarkBranchDeletedAppRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

#endregion Request Validators
