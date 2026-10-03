namespace LibraryTemplate.Application.Dependencies.Infrastructure;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
