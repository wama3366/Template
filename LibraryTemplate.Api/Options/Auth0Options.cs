using System.ComponentModel.DataAnnotations;

namespace LibraryTemplate.Api.Options;

public sealed class Auth0Options
{
    public const string SectionName = "Auth0";

    [Required]
    public string Domain { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;
}
