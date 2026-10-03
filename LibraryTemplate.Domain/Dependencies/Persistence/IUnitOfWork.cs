namespace LibraryTemplate.Domain.Dependencies.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
