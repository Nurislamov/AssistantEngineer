using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramFileLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanUseForDiagnostics",
                table: "TelegramManualBindings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "TelegramManualBindings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ServiceManual");

            migrationBuilder.AddColumn<bool>(
                name: "IsLibraryVisible",
                table: "TelegramManualBindings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MinRole",
                table: "TelegramManualBindings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Engineer");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TelegramManualBindings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TelegramLibraryAccessGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    GrantedByTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLibraryAccessGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramLibraryAccessRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Installer"),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByTelegramUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramLibraryAccessRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramManualBindings_Brand_Series_DocumentType_CanUseForD~",
                table: "TelegramManualBindings",
                columns: new[] { "Brand", "Series", "DocumentType", "CanUseForDiagnostics", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLibraryAccessGrants_GrantedByTelegramUserId",
                table: "TelegramLibraryAccessGrants",
                column: "GrantedByTelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLibraryAccessGrants_TelegramUserId_IsActive",
                table: "TelegramLibraryAccessGrants",
                columns: new[] { "TelegramUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLibraryAccessRequests_Status_CreatedAt",
                table: "TelegramLibraryAccessRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLibraryAccessRequests_TelegramChatId",
                table: "TelegramLibraryAccessRequests",
                column: "TelegramChatId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramLibraryAccessRequests_TelegramUserId_Status",
                table: "TelegramLibraryAccessRequests",
                columns: new[] { "TelegramUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramLibraryAccessGrants");

            migrationBuilder.DropTable(
                name: "TelegramLibraryAccessRequests");

            migrationBuilder.DropIndex(
                name: "IX_TelegramManualBindings_Brand_Series_DocumentType_CanUseForD~",
                table: "TelegramManualBindings");

            migrationBuilder.DropColumn(
                name: "CanUseForDiagnostics",
                table: "TelegramManualBindings");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "TelegramManualBindings");

            migrationBuilder.DropColumn(
                name: "IsLibraryVisible",
                table: "TelegramManualBindings");

            migrationBuilder.DropColumn(
                name: "MinRole",
                table: "TelegramManualBindings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TelegramManualBindings");
        }
    }
}
