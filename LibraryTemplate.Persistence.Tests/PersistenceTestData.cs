using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Domain.Services;
using LibraryTemplate.Persistence.Rows;
using LibraryTemplate.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraryTemplate.Persistence.Tests;

internal sealed record PersistenceTestData(
    BranchId BranchId,
    string BranchName,
    UserId AdultBorrowerUserId,
    UserId ChildBorrowerUserId,
    UserId BranchAdminUserId,
    UserId SystemAdminUserId,
    BookId GeneralBookId,
    BookId AdultOnlyBookId,
    string AdultBorrowerEmail);

internal static class PersistenceTestDataBuilder
{
    public static async Task ResetAsync(LibraryDbContext dbContext)
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
    }

    public static async Task<PersistenceTestData> CreateAsync(LibraryDbContext dbContext)
    {
        var suffix = Guid.CreateVersion7().ToString("N")[..8];
        var firstIsbnSuffix = Random.Shared.Next(100000, 899998);

        var branch = Branch.Create($"North Branch {suffix}");
        var libraryCatalogDomainService = new LibraryCatalogDomainService(
            new BookRepository(dbContext),
            new AuthorRepository(dbContext));
        var authorName = $"Template Author {suffix}";
        var (generalBook, _) = await libraryCatalogDomainService.AddBookAsync(
            $"The Clean Shelf {suffix}",
            authorName,
            Isbn.Create($"9780001{firstIsbnSuffix}"),
            AgeRestriction.General);
        var (adultOnlyBook, _) = await libraryCatalogDomainService.AddBookAsync(
            $"Restricted Stacks {suffix}",
            authorName,
            Isbn.Create($"9780001{firstIsbnSuffix + 1}"),
            AgeRestriction.AdultOnly);

        var adultBorrowerUserId = UserId.New();
        var childBorrowerUserId = UserId.New();
        var branchAdminUserId = UserId.New();
        var systemAdminUserId = UserId.New();
        var adultBorrowerEmail = $"adult.borrower.{suffix}@example.test";

        dbContext.Roles.AddRange(
            new RoleRow(UserRole.Borrower, "Can borrow allowed books in an assigned branch."),
            new RoleRow(UserRole.LibraryAdmin, "Can view borrowed books and borrowers in an assigned branch."),
            new RoleRow(UserRole.SystemAdmin, "Can administer the library across all branches."));

        dbContext.Branches.Add(branch);
        dbContext.Users.AddRange(
            User.Load(
                adultBorrowerUserId,
                $"auth0|adult-borrower-{suffix}",
                "Adult Borrower",
                adultBorrowerEmail,
                [UserRole.Borrower]),
            User.Load(
                childBorrowerUserId,
                $"auth0|child-borrower-{suffix}",
                "Child Borrower",
                $"child.borrower.{suffix}@example.test",
                [UserRole.Borrower]),
            User.Load(
                branchAdminUserId,
                $"auth0|branch-admin-{suffix}",
                "Branch Admin",
                $"branch.admin.{suffix}@example.test",
                [UserRole.LibraryAdmin]),
            User.Load(
                systemAdminUserId,
                $"auth0|system-admin-{suffix}",
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

        return new PersistenceTestData(
            branch.Id,
            branch.Name,
            adultBorrowerUserId,
            childBorrowerUserId,
            branchAdminUserId,
            systemAdminUserId,
            generalBook.Id,
            adultOnlyBook.Id,
            adultBorrowerEmail);
    }
}
