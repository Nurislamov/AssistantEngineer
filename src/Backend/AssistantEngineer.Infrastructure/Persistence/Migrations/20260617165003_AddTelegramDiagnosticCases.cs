using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramDiagnosticCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramDiagnosticCases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramConversationSessionId = table.Column<long>(type: "bigint", nullable: true),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserRoleAtCreation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResponseMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EquipmentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayContext = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResultSummary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NormalizedRequestJson = table.Column<string>(type: "jsonb", nullable: true),
                    CandidateCount = table.Column<int>(type: "integer", nullable: true),
                    PhoneWasSaved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhoneNumberSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramDiagnosticCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramDiagnosticCases_TelegramConversationSessions_Telegr~",
                        column: x => x.TelegramConversationSessionId,
                        principalTable: "TelegramConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TelegramDiagnosticCases_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiagnosticCases_CreatedAt",
                table: "TelegramDiagnosticCases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiagnosticCases_Status",
                table: "TelegramDiagnosticCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiagnosticCases_TelegramConversationSessionId",
                table: "TelegramDiagnosticCases",
                column: "TelegramConversationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiagnosticCases_TelegramUserId",
                table: "TelegramDiagnosticCases",
                column: "TelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiagnosticCases_TelegramUserId_CreatedAt",
                table: "TelegramDiagnosticCases",
                columns: new[] { "TelegramUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramDiagnosticCases");
        }
    }
}
