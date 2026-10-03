namespace LibraryTemplate.Domain.Aggregates.Branches;

public sealed class Branch : AggregateRoot<BranchId>
{
    private Branch()
    {
        Name = string.Empty;
    }

    private Branch(BranchId id, string name) : base(id)
    {
        Name = name;
    }

    public string Name { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string? DeletedReason { get; private set; }

    public static Branch Create(string name) =>
        new(BranchId.New(), name.Trim());

    public void MarkDeleted(string reason, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Branch.AlreadyDeleted", $"'{Name}' is already deleted.");
        }

        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedReason = reason.Trim();
    }
}

public readonly record struct BranchId(Guid Value)
{
    public static BranchId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
