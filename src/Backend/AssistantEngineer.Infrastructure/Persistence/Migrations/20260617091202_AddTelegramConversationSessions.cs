using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramConversationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramConversationSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SelectedManufacturer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SelectedEquipmentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SelectedDisplayContext = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CandidateOptionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastPromptMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramConversationSessions_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConversationSessions_ExpiresAt",
                table: "TelegramConversationSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConversationSessions_State",
                table: "TelegramConversationSessions",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConversationSessions_TelegramUserId",
                table: "TelegramConversationSessions",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramConversationSessions_UpdatedAt",
                table: "TelegramConversationSessions",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramConversationSessions");
        }
    }
}
