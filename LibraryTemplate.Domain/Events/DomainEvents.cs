namespace LibraryTemplate.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset RaisedAt { get; }
}

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
