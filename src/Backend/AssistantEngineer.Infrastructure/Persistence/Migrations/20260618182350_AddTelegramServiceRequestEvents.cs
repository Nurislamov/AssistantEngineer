using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequestEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramServiceRequestEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRequestId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    TargetTelegramUserId = table.Column<long>(type: "bigint", nullable: true),
                    OldStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramServiceRequestEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestEvents_TelegramServiceRequests_Servic~",
                        column: x => x.ServiceRequestId,
                        principalTable: "TelegramServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestEvents_TelegramUsers_ActorTelegramUse~",
                        column: x => x.ActorTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestEvents_TelegramUsers_TargetTelegramUs~",
                        column: x => x.TargetTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestEvents_ActorTelegramUserId_CreatedAt",
                table: "TelegramServiceRequestEvents",
                columns: new[] { "ActorTelegramUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestEvents_EventType_CreatedAt",
                table: "TelegramServiceRequestEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestEvents_ServiceRequestId_CreatedAt",
                table: "TelegramServiceRequestEvents",
                columns: new[] { "ServiceRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestEvents_TargetTelegramUserId",
                table: "TelegramServiceRequestEvents",
                column: "TargetTelegramUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramServiceRequestEvents");
        }
    }
}
