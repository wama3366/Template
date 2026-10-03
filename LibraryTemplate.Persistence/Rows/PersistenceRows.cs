using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Persistence.Rows;

public sealed class BookAuthorRow
{
    private BookAuthorRow()
    {
    }

    private BookAuthorRow(BookId bookId, AuthorId authorId)
    {
        BookId = bookId;
        AuthorId = authorId;
    }

    public BookId BookId { get; private set; }
    public AuthorId AuthorId { get; private set; }

    public static BookAuthorRow Create(BookId bookId, AuthorId authorId) =>
        new(bookId, authorId);
}

public sealed class RoleAssignmentRow
{
    private RoleAssignmentRow()
    {
    }

    private RoleAssignmentRow(Guid id, UserId userId, UserRole role)
    {
        Id = id;
        UserId = userId;
        Role = role;
    }

    public Guid Id { get; private set; }
    public UserId UserId { get; private set; }
    public UserRole Role { get; private set; }

    public static RoleAssignmentRow Create(UserId userId, UserRole role) =>
        new(Guid.CreateVersion7(), userId, role);
}

public sealed class UserBranchRow
{
    private UserBranchRow()
    {
    }

    private UserBranchRow(UserId userId, BranchId branchId)
    {
        UserId = userId;
        BranchId = branchId;
    }

    public UserId UserId { get; private set; }
    public BranchId BranchId { get; private set; }

    public static UserBranchRow Create(UserId userId, BranchId branchId) =>
        new(userId, branchId);
}

public sealed class RoleRow
{
    private RoleRow() : this(default, string.Empty)
    {
    }

    public RoleRow(UserRole name, string description)
    {
        Name = name;
        Description = description;
    }

    public UserRole Name { get; private set; }
    public string Description { get; private set; }
}
