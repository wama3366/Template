namespace LibraryTemplate.Domain.Aggregates.Users;

public sealed class BorrowerProfile
{
    private BorrowerProfile()
    {
    }

    public BorrowerProfile(UserId userId, BorrowerCategory category)
    {
        UserId = userId;
        Category = category;
    }

    public UserId UserId { get; private set; }
    public BorrowerCategory Category { get; private set; }
}
