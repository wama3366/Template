using FluentAssertions;
using LibraryTemplate.Domain.Aggregates.Branches;

namespace LibraryTemplate.Domain.Tests;

public sealed class BranchTests
{
    [Fact]
    public void Mark_deleted_sets_deleted_state()
    {
        // Arrange
        var branch = Branch.Create("North Branch");
        var deletedAt = new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);

        // Act
        branch.MarkDeleted("Closed for renovation.", deletedAt);

        // Assert
        branch.IsDeleted.Should().BeTrue();
        branch.DeletedAt.Should().Be(deletedAt);
        branch.DeletedReason.Should().Be("Closed for renovation.");
    }

    [Fact]
    public void Mark_deleted_fails_when_branch_is_already_deleted()
    {
        // Arrange
        var branch = Branch.Create("North Branch");
        branch.MarkDeleted("Closed for renovation.", DateTimeOffset.UtcNow);

        // Act
        var act = () => branch.MarkDeleted("Second delete.", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already deleted*");
    }
}
