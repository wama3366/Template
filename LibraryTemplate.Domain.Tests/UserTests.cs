using FluentAssertions;
using LibraryTemplate.Domain.Aggregates.Users;

namespace LibraryTemplate.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Has_role_matches_non_branch_scoped_assignment()
    {
        // Arrange
        var user = User.Load(
            UserId.New(),
            "System Admin",
            "system@example.test",
            [UserRole.SystemAdmin]);

        // Act
        var hasSystemAdminRole = user.HasRole(UserRole.SystemAdmin);
        var hasLibraryAdminRole = user.HasRole(UserRole.LibraryAdmin);

        // Assert
        hasSystemAdminRole.Should().BeTrue();
        hasLibraryAdminRole.Should().BeFalse();
    }
}
