using FluentAssertions;
using LibraryTemplate.Application.Dependencies.Infrastructure;
using LibraryTemplate.Application.Services.Branches;
using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Dependencies.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LibraryTemplate.Application.Tests;

public sealed class BranchAppServiceTests
{
    [Fact]
    public async Task System_admin_role_is_required_before_branch_is_loaded()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var userRepository = Substitute.For<IUserRepository>();
        var branchRepository = Substitute.For<IBranchRepository>();
        var service = CreateService(
            userRepository,
            branchRepository,
            Substitute.For<IUnitOfWork>());

        userRepository.FindByExternalUserIdAsync("external-user-1")
            .Returns(UserWithRoles(UserRole.LibraryAdmin));

        // Act
        var act = () => service.MarkDeletedAsync(
            branchId,
            new MarkBranchDeletedAppRequest { Reason = "Closed." });

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        await branchRepository.DidNotReceive().FindByIdAsync(Arg.Any<BranchId>());
    }

    [Fact]
    public async Task Missing_user_fails_before_branch_is_loaded()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var userRepository = Substitute.For<IUserRepository>();
        var branchRepository = Substitute.For<IBranchRepository>();
        var service = CreateService(
            userRepository,
            branchRepository,
            Substitute.For<IUnitOfWork>());

        // Act
        var act = () => service.MarkDeletedAsync(
            branchId,
            new MarkBranchDeletedAppRequest { Reason = "Closed." });

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
        await branchRepository.DidNotReceive().FindByIdAsync(Arg.Any<BranchId>());
    }

    [Fact]
    public async Task Missing_branch_fails_with_not_found()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.FindByExternalUserIdAsync("external-user-1")
            .Returns(UserWithRoles(UserRole.SystemAdmin));
        var branchRepository = Substitute.For<IBranchRepository>();

        var service = CreateService(
            userRepository,
            branchRepository,
            Substitute.For<IUnitOfWork>());

        // Act
        var act = () => service.MarkDeletedAsync(
            branchId,
            new MarkBranchDeletedAppRequest { Reason = "Closed." });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Successful_call_marks_branch_deleted_and_saves()
    {
        // Arrange
        var branchId = Guid.CreateVersion7();
        var deletedAt = new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);
        var branch = Branch.Create("North Branch");
        var userRepository = Substitute.For<IUserRepository>();
        var branchRepository = Substitute.For<IBranchRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = CreateService(
            userRepository,
            branchRepository,
            unitOfWork,
            deletedAt);

        userRepository.FindByExternalUserIdAsync("external-user-1")
            .Returns(UserWithRoles(UserRole.SystemAdmin));
        branchRepository.FindByIdAsync(new BranchId(branchId)).Returns(branch);

        // Act
        var response = await service.MarkDeletedAsync(
            branchId,
            new MarkBranchDeletedAppRequest { Reason = "Closed for renovation." });

        // Assert
        response.IsDeleted.Should().BeTrue();
        response.DeletedAt.Should().Be(deletedAt);
        response.DeletedReason.Should().Be("Closed for renovation.");
        branch.IsDeleted.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync();
    }

    private static BranchAppService CreateService(
        IUserRepository userRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        DateTimeOffset? utcNow = null)
    {
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.ExternalUserId.Returns("external-user-1");

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(utcNow ?? new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero));

        return new BranchAppService(
            new MarkBranchDeletedAppRequestValidator(),
            currentUserProvider,
            userRepository,
            branchRepository,
            unitOfWork,
            clock,
            NullLogger<BranchAppService>.Instance);
    }

    private static User UserWithRoles(params UserRole[] roles) =>
        User.Load(
            UserId.New(),
            "Test User",
            "user@example.test",
            roles);
}
