using System.ComponentModel.DataAnnotations;

namespace LibraryTemplate.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = "library@example.test";
}
