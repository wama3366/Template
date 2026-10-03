namespace LibraryTemplate.Domain.Aggregates.Users;

public sealed class User : AggregateRoot<UserId>
{
    private readonly List<UserRole> _roles = [];

    private User()
    {
        ExternalUserId = string.Empty;
        DisplayName = string.Empty;
        Email = string.Empty;
    }

    private User(
        UserId id,
        string externalUserId,
        string displayName,
        string email,
        IReadOnlyCollection<UserRole> roles) : base(id)
    {
        ExternalUserId = externalUserId;
        DisplayName = displayName;
        Email = email;
        _roles.AddRange(roles);
    }

    public string ExternalUserId { get; private set; }
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public static User Load(
        UserId id,
        string displayName,
        string email,
        IReadOnlyCollection<UserRole> roles) =>
        new(id, string.Empty, displayName, email, roles);

    public static User Load(
        UserId id,
        string externalUserId,
        string displayName,
        string email,
        IReadOnlyCollection<UserRole> roles) =>
        new(id, externalUserId, displayName, email, roles);

    public bool HasRole(UserRole role) =>
        _roles.Contains(role);
}

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
