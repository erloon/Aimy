using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "uploads",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_uploads_UserId_FileName",
                table: "uploads",
                columns: new[] { "UserId", "FileName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_uploads_UserId_FileName",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "uploads");
        }
    }
}
