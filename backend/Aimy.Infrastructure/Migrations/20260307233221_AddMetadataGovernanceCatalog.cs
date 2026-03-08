using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aimy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataGovernanceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metadata_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Filterable = table.Column<bool>(type: "boolean", nullable: false),
                    AllowFreeText = table.Column<bool>(type: "boolean", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    Policy = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metadata_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metadata_value_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetadataDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metadata_value_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metadata_value_options_metadata_definitions_MetadataDefinit~",
                        column: x => x.MetadataDefinitionId,
                        principalTable: "metadata_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_metadata_definitions_Key",
                table: "metadata_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metadata_value_options_CanonicalValue",
                table: "metadata_value_options",
                column: "CanonicalValue");

            migrationBuilder.CreateIndex(
                name: "IX_metadata_value_options_MetadataDefinitionId",
                table: "metadata_value_options",
                column: "MetadataDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_metadata_value_options_MetadataDefinitionId_CanonicalValue",
                table: "metadata_value_options",
                columns: new[] { "MetadataDefinitionId", "CanonicalValue" },
                unique: true);

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_metadata_definitions_KeyPrefix\" ON metadata_definitions ((lower(\"Key\")));");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_metadata_value_options_CanonicalPrefix\" ON metadata_value_options ((lower(\"CanonicalValue\")));");

            migrationBuilder.Sql(@"
INSERT INTO metadata_definitions (""Id"", ""Key"", ""Label"", ""ValueType"", ""Filterable"", ""AllowFreeText"", ""Required"", ""Policy"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
VALUES
    ('a091f2b2-4cde-4f5b-b207-0ec11a3d6d1a', 'framework', 'Framework', 'string', true, false, false, 1, true, now(), now()),
    ('3ca80b43-fef8-4db9-b75f-11de4b5f66e9', 'language', 'Language', 'string', true, false, false, 1, true, now(), now()),
    ('6b14e955-2d9d-4f67-89f7-b870025f7e7b', 'source', 'Source', 'string', true, true, false, 2, true, now(), now())
ON CONFLICT (""Key"") DO UPDATE
SET
    ""Label"" = EXCLUDED.""Label"",
    ""ValueType"" = EXCLUDED.""ValueType"",
    ""Filterable"" = EXCLUDED.""Filterable"",
    ""AllowFreeText"" = EXCLUDED.""AllowFreeText"",
    ""Required"" = EXCLUDED.""Required"",
    ""Policy"" = EXCLUDED.""Policy"",
    ""IsActive"" = EXCLUDED.""IsActive"",
    ""UpdatedAt"" = now();

INSERT INTO metadata_value_options (""Id"", ""MetadataDefinitionId"", ""CanonicalValue"", ""DisplayLabel"", ""Aliases"", ""IsActive"", ""SortOrder"", ""CreatedAt"", ""UpdatedAt"")
VALUES
    ('ca521d7b-c349-4cb9-b17d-ac8d3fcf58ea', 'a091f2b2-4cde-4f5b-b207-0ec11a3d6d1a', 'microsoft.agents', 'Microsoft Agents', ARRAY['ms agents', 'Microsoft.Agents'], true, 10, now(), now()),
    ('d9f29d8a-da53-45fd-9d4a-ebf74ee6f196', 'a091f2b2-4cde-4f5b-b207-0ec11a3d6d1a', 'aspnet.core', 'ASP.NET Core', ARRAY['asp.net core', 'aspnet'], true, 20, now(), now()),
    ('6b7c2808-7f15-4c71-a1d7-f126f03c5823', '3ca80b43-fef8-4db9-b75f-11de4b5f66e9', 'csharp', 'C#', ARRAY['c#', 'cs'], true, 10, now(), now()),
    ('cf3cb6be-c72a-43e6-b9b0-f3ee4864ded6', '3ca80b43-fef8-4db9-b75f-11de4b5f66e9', 'typescript', 'TypeScript', ARRAY['ts', 'type script'], true, 20, now(), now()),
    ('a3700f7f-5d16-4c0f-89f7-cda4b4f1289c', '6b14e955-2d9d-4f67-89f7-b870025f7e7b', 'upload', 'Upload', ARRAY['file upload'], true, 10, now(), now()),
    ('dc93f879-715f-4d1d-8f48-8fd17ea2f2da', '6b14e955-2d9d-4f67-89f7-b870025f7e7b', 'note', 'Note', ARRAY['kb note'], true, 20, now(), now())
ON CONFLICT (""MetadataDefinitionId"", ""CanonicalValue"") DO UPDATE
SET
    ""DisplayLabel"" = EXCLUDED.""DisplayLabel"",
    ""Aliases"" = EXCLUDED.""Aliases"",
    ""IsActive"" = EXCLUDED.""IsActive"",
    ""SortOrder"" = EXCLUDED.""SortOrder"",
    ""UpdatedAt"" = now();

");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metadata_value_options");

            migrationBuilder.DropTable(
                name: "metadata_definitions");
        }
    }
}
