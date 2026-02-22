using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_embeddings",
                columns: table => new
                {
                    key = table.Column<Guid>(type: "uuid", nullable: false),
                    documentid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sourceid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(3072)", nullable: true),
                    context = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_embeddings", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_embeddings_documentid",
                table: "ingestion_embeddings",
                column: "documentid");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_embeddings_sourceid",
                table: "ingestion_embeddings",
                column: "sourceid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_embeddings");
        }
    }
}
