using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAndBranchDeletionPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branches_IsDeleted",
                table: "branches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_branches_Name",
                table: "branches",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branches");
        }
    }
}
