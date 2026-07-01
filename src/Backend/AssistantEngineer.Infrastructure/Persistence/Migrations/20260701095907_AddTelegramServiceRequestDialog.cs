using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequestDialog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramServiceRequestMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRequestId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SenderTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    SenderRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramServiceRequestMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestMessages_TelegramServiceRequests_Serv~",
                        column: x => x.ServiceRequestId,
                        principalTable: "TelegramServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestMessages_TelegramUsers_SenderTelegram~",
                        column: x => x.SenderTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TelegramServiceRequestPending",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceRequestId = table.Column<long>(type: "bigint", nullable: true),
                    PendingText = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramServiceRequestPending", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestPending_TelegramServiceRequests_Servi~",
                        column: x => x.ServiceRequestId,
                        principalTable: "TelegramServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestPending_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestMessages_SenderTelegramUserId_Created~",
                table: "TelegramServiceRequestMessages",
                columns: new[] { "SenderTelegramUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestMessages_ServiceRequestId_CreatedAt",
                table: "TelegramServiceRequestMessages",
                columns: new[] { "ServiceRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestPending_ServiceRequestId",
                table: "TelegramServiceRequestPending",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestPending_TelegramUserId",
                table: "TelegramServiceRequestPending",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramServiceRequestMessages");

            migrationBuilder.DropTable(
                name: "TelegramServiceRequestPending");
        }
    }
}
