using LibraryTemplate.Domain.Events;

namespace LibraryTemplate.Application.Events;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent);
}
