using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryTemplate.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_authors_Name",
                table: "authors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_authors_Name",
                table: "authors");
        }
    }
}
