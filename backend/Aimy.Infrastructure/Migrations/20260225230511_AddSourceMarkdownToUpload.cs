using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceMarkdownToUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceMarkdown",
                table: "uploads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceMarkdown",
                table: "uploads");
        }
    }
}
