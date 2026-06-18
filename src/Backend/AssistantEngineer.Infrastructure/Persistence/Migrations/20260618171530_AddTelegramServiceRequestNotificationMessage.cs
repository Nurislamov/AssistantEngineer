using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequestNotificationMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NotificationChatId",
                table: "TelegramServiceRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NotificationMessageId",
                table: "TelegramServiceRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NotificationSentAt",
                table: "TelegramServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NotificationUpdatedAt",
                table: "TelegramServiceRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationChatId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "NotificationMessageId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "NotificationSentAt",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "NotificationUpdatedAt",
                table: "TelegramServiceRequests");
        }
    }
}
