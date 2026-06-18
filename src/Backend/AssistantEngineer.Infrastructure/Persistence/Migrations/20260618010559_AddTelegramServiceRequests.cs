using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramServiceRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    DiagnosticCaseId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EquipmentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayContext = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PhoneWasSaved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhoneNumberSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ContactPhoneLast4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    UserRoleAtCreation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequests_TelegramDiagnosticCases_DiagnosticC~",
                        column: x => x.DiagnosticCaseId,
                        principalTable: "TelegramDiagnosticCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequests_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_ActiveDiagnosticCase",
                table: "TelegramServiceRequests",
                column: "DiagnosticCaseId",
                unique: true,
                filter: "\"Status\" IN ('New', 'InProgress')");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_DiagnosticCaseId_Status",
                table: "TelegramServiceRequests",
                columns: new[] { "DiagnosticCaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_Status",
                table: "TelegramServiceRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_TelegramUserId",
                table: "TelegramServiceRequests",
                column: "TelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_TelegramUserId_CreatedAt",
                table: "TelegramServiceRequests",
                columns: new[] { "TelegramUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramServiceRequests");
        }
    }
}
