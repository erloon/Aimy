using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyKnowledgeItemMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "knowledge_items",
                newName: "Metadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "knowledge_items",
                newName: "Tags");
        }
    }
}
