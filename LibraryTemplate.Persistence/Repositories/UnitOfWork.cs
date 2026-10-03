using LibraryTemplate.Application.Events;
using LibraryTemplate.Domain.Dependencies.Persistence;
using LibraryTemplate.Domain.Events;

namespace LibraryTemplate.Persistence.Repositories;

public sealed class UnitOfWork(
    LibraryDbContext dbContext,
    IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
{
    public async Task SaveChangesAsync()
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = aggregates
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        await dbContext.SaveChangesAsync();

        if (domainEvents.Length == 0)
        {
            return;
        }

        await domainEventDispatcher.DispatchAsync(domainEvents);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
