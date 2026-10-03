using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Persistence;
using LibraryTemplate.Persistence.Rows;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Api.IntegrationTests;

public sealed record IntegrationTestData(
    BranchId BranchId,
    UserId AdultBorrowerUserId,
    string AdultBorrowerSubject,
    string AdultBorrowerEmail,
    string ChildBorrowerSubject,
    string BranchAdminSubject,
    string SystemAdminSubject);

internal static class IntegrationTestDataBuilder
{
    public static async Task<IntegrationTestData> ResetAndCreateAsync(LibraryDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                loan_lines,
                loans,
                book_authors,
                books,
                authors,
                user_branches,
                role_assignments,
                borrower_profiles,
                app_users,
                roles,
                branches
            RESTART IDENTITY CASCADE;
            """);

        var suffix = Guid.CreateVersion7().ToString("N")[..8];
        var branch = Branch.Create($"North Branch {suffix}");
        var adultBorrowerUserId = UserId.New();
        var childBorrowerUserId = UserId.New();
        var branchAdminUserId = UserId.New();
        var systemAdminUserId = UserId.New();
        var adultBorrowerSubject = $"auth0|adult-borrower-{suffix}";
        var childBorrowerSubject = $"auth0|child-borrower-{suffix}";
        var branchAdminSubject = $"auth0|branch-admin-{suffix}";
        var systemAdminSubject = $"auth0|system-admin-{suffix}";
        var adultBorrowerEmail = $"adult.borrower.{suffix}@example.test";

        dbContext.Roles.AddRange(
            new RoleRow(UserRole.Borrower, "Can borrow allowed books in an assigned branch."),
            new RoleRow(UserRole.LibraryAdmin, "Can view borrowed books and borrowers in an assigned branch."),
            new RoleRow(UserRole.SystemAdmin, "Can administer the library across all branches."));

        dbContext.Branches.Add(branch);
        dbContext.Users.AddRange(
            User.Load(
                adultBorrowerUserId,
                adultBorrowerSubject,
                "Adult Borrower",
                adultBorrowerEmail,
                [UserRole.Borrower]),
            User.Load(
                childBorrowerUserId,
                childBorrowerSubject,
                "Child Borrower",
                $"child.borrower.{suffix}@example.test",
                [UserRole.Borrower]),
            User.Load(
                branchAdminUserId,
                branchAdminSubject,
                "Branch Admin",
                $"branch.admin.{suffix}@example.test",
                [UserRole.LibraryAdmin]),
            User.Load(
                systemAdminUserId,
                systemAdminSubject,
                "System Admin",
                $"system.admin.{suffix}@example.test",
                [UserRole.SystemAdmin]));

        dbContext.RoleAssignments.AddRange(
            RoleAssignmentRow.Create(adultBorrowerUserId, UserRole.Borrower),
            RoleAssignmentRow.Create(childBorrowerUserId, UserRole.Borrower),
            RoleAssignmentRow.Create(branchAdminUserId, UserRole.LibraryAdmin),
            RoleAssignmentRow.Create(systemAdminUserId, UserRole.SystemAdmin));

        dbContext.BorrowerProfiles.AddRange(
            new BorrowerProfile(adultBorrowerUserId, BorrowerCategory.Adult),
            new BorrowerProfile(childBorrowerUserId, BorrowerCategory.Child),
            new BorrowerProfile(branchAdminUserId, BorrowerCategory.Adult),
            new BorrowerProfile(systemAdminUserId, BorrowerCategory.Adult));

        dbContext.UserBranches.AddRange(
            UserBranchRow.Create(adultBorrowerUserId, branch.Id),
            UserBranchRow.Create(childBorrowerUserId, branch.Id),
            UserBranchRow.Create(branchAdminUserId, branch.Id));

        await dbContext.SaveChangesAsync();

        return new IntegrationTestData(
            branch.Id,
            adultBorrowerUserId,
            adultBorrowerSubject,
            adultBorrowerEmail,
            childBorrowerSubject,
            branchAdminSubject,
            systemAdminSubject);
    }
}
