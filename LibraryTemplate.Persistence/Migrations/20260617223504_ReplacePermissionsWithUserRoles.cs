using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePermissionsWithUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "roles");

            migrationBuilder.RenameTable(
                name: "user_branch_roles",
                newName: "role_assignments");

            migrationBuilder.RenameIndex(
                name: "IX_user_branch_roles_RoleName",
                table: "role_assignments",
                newName: "IX_role_assignments_RoleName");

            migrationBuilder.RenameIndex(
                name: "IX_user_branch_roles_UserId_BranchId_RoleName",
                table: "role_assignments",
                newName: "IX_role_assignments_UserId_BranchId_RoleName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "role_assignments",
                newName: "user_branch_roles");

            migrationBuilder.RenameIndex(
                name: "IX_role_assignments_RoleName",
                table: "user_branch_roles",
                newName: "IX_user_branch_roles_RoleName");

            migrationBuilder.RenameIndex(
                name: "IX_role_assignments_UserId_BranchId_RoleName",
                table: "user_branch_roles",
                newName: "IX_user_branch_roles_UserId_BranchId_RoleName");

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PermissionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleName, x.PermissionName });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionName",
                        column: x => x.PermissionName,
                        principalTable: "permissions",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleName",
                        column: x => x.RoleName,
                        principalTable: "roles",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionName",
                table: "role_permissions",
                column: "PermissionName");
        }
    }
}
