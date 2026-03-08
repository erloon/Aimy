using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableIngestionJobsAndUploadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "ingestion_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingestion_jobs_uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_uploads_IngestionStatus",
                table: "uploads",
                column: "IngestionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_NextAttemptAt",
                table: "ingestion_jobs",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_Status",
                table: "ingestion_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_UploadId",
                table: "ingestion_jobs",
                column: "UploadId",
                unique: true,
                filter: "\"Status\" IN (0, 1)");

            migrationBuilder.Sql(@"
                UPDATE uploads AS u
                SET ""IngestionStatus"" = 2,
                    ""IngestionCompletedAt"" = COALESCE(u.""DateUploaded"", NOW())
                WHERE EXISTS (
                    SELECT 1
                    FROM ingestion_embeddings AS ie
                    WHERE ie.sourceid = u.""Id""::text
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ingestion_jobs (
                    ""Id"",
                    ""UploadId"",
                    ""Status"",
                    ""Attempts"",
                    ""NextAttemptAt"",
                    ""ClaimedAt"",
                    ""CompletedAt"",
                    ""LastError"",
                    ""CreatedAt"",
                    ""UpdatedAt""
                )
                SELECT
                    u.""Id"",
                    u.""Id"",
                    0,
                    0,
                    NOW(),
                    NULL,
                    NULL,
                    NULL,
                    NOW(),
                    NOW()
                FROM uploads AS u
                WHERE u.""IngestionStatus"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_jobs");

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
        }
    }
}
