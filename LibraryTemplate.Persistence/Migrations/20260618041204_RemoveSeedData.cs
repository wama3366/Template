using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LibraryTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "book_authors",
                keyColumns: new[] { "AuthorId", "BookId" },
                keyValues: new object[] { new Guid("01971f00-0000-7000-8000-000000000301"), new Guid("01971f00-0000-7000-8000-000000000201") });

            migrationBuilder.DeleteData(
                table: "book_authors",
                keyColumns: new[] { "AuthorId", "BookId" },
                keyValues: new object[] { new Guid("01971f00-0000-7000-8000-000000000301"), new Guid("01971f00-0000-7000-8000-000000000202") });

            migrationBuilder.DeleteData(
                table: "borrower_profiles",
                keyColumn: "AppUserId",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000101"));

            migrationBuilder.DeleteData(
                table: "borrower_profiles",
                keyColumn: "AppUserId",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000102"));

            migrationBuilder.DeleteData(
                table: "borrower_profiles",
                keyColumn: "AppUserId",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000103"));

            migrationBuilder.DeleteData(
                table: "borrower_profiles",
                keyColumn: "AppUserId",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000104"));

            migrationBuilder.DeleteData(
                table: "role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000401"));

            migrationBuilder.DeleteData(
                table: "role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000402"));

            migrationBuilder.DeleteData(
                table: "role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000403"));

            migrationBuilder.DeleteData(
                table: "role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000404"));

            migrationBuilder.DeleteData(
                table: "user_branches",
                keyColumns: new[] { "BranchId", "UserId" },
                keyValues: new object[] { new Guid("01971f00-0000-7000-8000-000000000001"), new Guid("01971f00-0000-7000-8000-000000000101") });

            migrationBuilder.DeleteData(
                table: "user_branches",
                keyColumns: new[] { "BranchId", "UserId" },
                keyValues: new object[] { new Guid("01971f00-0000-7000-8000-000000000001"), new Guid("01971f00-0000-7000-8000-000000000102") });

            migrationBuilder.DeleteData(
                table: "user_branches",
                keyColumns: new[] { "BranchId", "UserId" },
                keyValues: new object[] { new Guid("01971f00-0000-7000-8000-000000000001"), new Guid("01971f00-0000-7000-8000-000000000103") });

            migrationBuilder.DeleteData(
                table: "app_users",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000101"));

            migrationBuilder.DeleteData(
                table: "app_users",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000102"));

            migrationBuilder.DeleteData(
                table: "app_users",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000103"));

            migrationBuilder.DeleteData(
                table: "app_users",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000104"));

            migrationBuilder.DeleteData(
                table: "authors",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000301"));

            migrationBuilder.DeleteData(
                table: "books",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000201"));

            migrationBuilder.DeleteData(
                table: "books",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000202"));

            migrationBuilder.DeleteData(
                table: "branches",
                keyColumn: "Id",
                keyValue: new Guid("01971f00-0000-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Name",
                keyValue: "Borrower");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Name",
                keyValue: "LibraryAdmin");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Name",
                keyValue: "SystemAdmin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
