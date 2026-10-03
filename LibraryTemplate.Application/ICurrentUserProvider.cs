namespace LibraryTemplate.Application;

public interface ICurrentUserProvider
{
    string ExternalUserId { get; }
}
