using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBroadcastAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramBroadcastAttachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileUniqueId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBroadcastAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramBroadcastAttachments_TelegramBroadcastCampaigns_Cam~",
                        column: x => x.CampaignId,
                        principalTable: "TelegramBroadcastCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastAttachments_CampaignId",
                table: "TelegramBroadcastAttachments",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramBroadcastAttachments_CampaignId_SortOrder",
                table: "TelegramBroadcastAttachments",
                columns: new[] { "CampaignId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramBroadcastAttachments");
        }
    }
}
