using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequestDialogAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramServiceRequestMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileUniqueId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramServiceRequestMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramServiceRequestMessageAttachments_TelegramServiceReq~",
                        column: x => x.MessageId,
                        principalTable: "TelegramServiceRequestMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestMessageAttachments_MessageId",
                table: "TelegramServiceRequestMessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequestMessageAttachments_MessageId_Attachme~",
                table: "TelegramServiceRequestMessageAttachments",
                columns: new[] { "MessageId", "AttachmentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramServiceRequestMessageAttachments");
        }
    }
}
