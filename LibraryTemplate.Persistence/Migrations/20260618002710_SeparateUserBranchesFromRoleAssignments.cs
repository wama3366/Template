using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateUserBranchesFromRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_branches",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_branches", x => new { x.UserId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_user_branches_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_branches_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO user_branches ("UserId", "BranchId")
                SELECT DISTINCT "UserId", "BranchId"
                FROM role_assignments
                WHERE "BranchId" IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropIndex(
                name: "IX_role_assignments_UserId_BranchId_RoleName",
                table: "role_assignments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "role_assignments");

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_UserId_RoleName",
                table: "role_assignments",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_branches_BranchId",
                table: "user_branches",
                column: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_role_assignments_UserId_RoleName",
                table: "role_assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "role_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE role_assignments AS role_assignment
                SET "BranchId" = branch_assignment."BranchId"
                FROM (
                    SELECT DISTINCT ON ("UserId") "UserId", "BranchId"
                    FROM user_branches
                    ORDER BY "UserId", "BranchId"
                ) AS branch_assignment
                WHERE role_assignment."UserId" = branch_assignment."UserId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_role_assignments_UserId_BranchId_RoleName",
                table: "role_assignments",
                columns: new[] { "UserId", "BranchId", "RoleName" },
                unique: true);

            migrationBuilder.DropTable(
                name: "user_branches");
        }
    }
}
