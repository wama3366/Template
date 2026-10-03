using LibraryTemplate.Application.Dependencies.Infrastructure;

namespace LibraryTemplate.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
