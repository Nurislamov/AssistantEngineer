using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBroadcasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramBroadcastCampaigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedByTelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByTelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    AudienceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AudienceRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBroadcastCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramBroadcastCampaigns_TelegramUsers_CreatedByTelegramU~",
                        column: x => x.CreatedByTelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TelegramBroadcastRecipients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SkipReason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBroadcastRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramBroadcastRecipients_TelegramBroadcastCampaigns_Camp~",
                        column: x => x.CampaignId,
                        principalTable: "TelegramBroadcastCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelegramBroadcastRecipients_TelegramUsers_TelegramUserId",
                        column: x => x.TelegramUserId,
                        principalTable: "TelegramUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastCampaigns_AudienceKind_AudienceRole",
                table: "TelegramBroadcastCampaigns",
                columns: new[] { "AudienceKind", "AudienceRole" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastCampaigns_CreatedAt",
                table: "TelegramBroadcastCampaigns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastCampaigns_CreatedByTelegramUserId",
                table: "TelegramBroadcastCampaigns",
                column: "CreatedByTelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastCampaigns_Status",
                table: "TelegramBroadcastCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastRecipients_CampaignId",
                table: "TelegramBroadcastRecipients",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastRecipients_CampaignId_Status",
                table: "TelegramBroadcastRecipients",
                columns: new[] { "CampaignId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastRecipients_TelegramUserId",
                table: "TelegramBroadcastRecipients",
                column: "TelegramUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramBroadcastRecipients");

            migrationBuilder.DropTable(
                name: "TelegramBroadcastCampaigns");
        }
    }
}
