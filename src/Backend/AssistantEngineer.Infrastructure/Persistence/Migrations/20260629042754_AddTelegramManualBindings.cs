using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramManualBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramManualBindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ManualId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Series = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TelegramFileId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TelegramFileUniqueId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByTelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    RegisteredByRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramManualBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramManualBindings_Brand_Series_IsActive",
                table: "TelegramManualBindings",
                columns: new[] { "Brand", "Series", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramManualBindings_ManualId",
                table: "TelegramManualBindings",
                column: "ManualId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramManualBindings");
        }
    }
}
