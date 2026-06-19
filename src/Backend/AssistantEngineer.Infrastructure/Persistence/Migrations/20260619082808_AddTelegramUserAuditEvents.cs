using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramUserAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramUserAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    TargetTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    OldRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OldIsEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    NewIsEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    OldIsBlocked = table.Column<bool>(type: "boolean", nullable: true),
                    NewIsBlocked = table.Column<bool>(type: "boolean", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUserAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramUserAuditEvents_TelegramUsers_ActorTelegramUserId",
                        column: x => x.ActorTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TelegramUserAuditEvents_TelegramUsers_TargetTelegramUserId",
                        column: x => x.TargetTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserAuditEvents_ActorTelegramUserId_CreatedAt",
                table: "TelegramUserAuditEvents",
                columns: new[] { "ActorTelegramUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserAuditEvents_CreatedAt_Id",
                table: "TelegramUserAuditEvents",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserAuditEvents_EventType_CreatedAt",
                table: "TelegramUserAuditEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserAuditEvents_TargetTelegramUserId_CreatedAt",
                table: "TelegramUserAuditEvents",
                columns: new[] { "TargetTelegramUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramUserAuditEvents");
        }
    }
}
