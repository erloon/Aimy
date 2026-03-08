using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIngestionFieldsFromUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_uploads_IngestionStatus",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "IngestionCompletedAt",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "IngestionError",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "IngestionStartedAt",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "IngestionStatus",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "SourceMarkdown",
                table: "uploads");

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "ingestion_jobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "started_at",
                table: "ingestion_jobs");

            migrationBuilder.AddColumn<DateTime>(
                name: "IngestionCompletedAt",
                table: "uploads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IngestionError",
                table: "uploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IngestionStartedAt",
                table: "uploads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IngestionStatus",
                table: "uploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceMarkdown",
                table: "uploads",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_uploads_IngestionStatus",
                table: "uploads",
                column: "IngestionStatus");
        }
    }
}
