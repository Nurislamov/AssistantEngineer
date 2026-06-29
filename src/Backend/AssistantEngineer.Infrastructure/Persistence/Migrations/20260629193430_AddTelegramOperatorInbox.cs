using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramOperatorInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramOperatorInboxThreads",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUserMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastOwnerReplyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramOperatorInboxThreads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramOperatorInboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThreadId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserChatId = table.Column<long>(type: "bigint", nullable: true),
                    UserMessageId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorChatId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorMessageId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorReplyToMessageId = table.Column<long>(type: "bigint", nullable: true),
                    MessageKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramOperatorInboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramOperatorInboxMessages_TelegramOperatorInboxThreads_~",
                        column: x => x.ThreadId,
                        principalTable: "TelegramOperatorInboxThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxMessages_CreatedAt",
                table: "TelegramOperatorInboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxMessages_OperatorChatId_OperatorMessag~",
                table: "TelegramOperatorInboxMessages",
                columns: new[] { "OperatorChatId", "OperatorMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxMessages_OperatorChatId_OperatorReplyT~",
                table: "TelegramOperatorInboxMessages",
                columns: new[] { "OperatorChatId", "OperatorReplyToMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxMessages_ThreadId",
                table: "TelegramOperatorInboxMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxMessages_UserChatId_UserMessageId",
                table: "TelegramOperatorInboxMessages",
                columns: new[] { "UserChatId", "UserMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxThreads_CreatedAt",
                table: "TelegramOperatorInboxThreads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramOperatorInboxThreads_TelegramChatId",
                table: "TelegramOperatorInboxThreads",
                column: "TelegramChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramOperatorInboxMessages");

            migrationBuilder.DropTable(
                name: "TelegramOperatorInboxThreads");
        }
    }
}
