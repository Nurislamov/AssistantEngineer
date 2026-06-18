using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramServiceRequestAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "TelegramServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignedByTelegramUserId",
                table: "TelegramServiceRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignedTelegramUserId",
                table: "TelegramServiceRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusUpdatedAt",
                table: "TelegramServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StatusUpdatedByTelegramUserId",
                table: "TelegramServiceRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_AssignedByTelegramUserId",
                table: "TelegramServiceRequests",
                column: "AssignedByTelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_AssignedTelegramUserId",
                table: "TelegramServiceRequests",
                column: "AssignedTelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramServiceRequests_StatusUpdatedByTelegramUserId",
                table: "TelegramServiceRequests",
                column: "StatusUpdatedByTelegramUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_AssignedByTelegramUse~",
                table: "TelegramServiceRequests",
                column: "AssignedByTelegramUserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_AssignedTelegramUserId",
                table: "TelegramServiceRequests",
                column: "AssignedTelegramUserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_StatusUpdatedByTelegr~",
                table: "TelegramServiceRequests",
                column: "StatusUpdatedByTelegramUserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_AssignedByTelegramUse~",
                table: "TelegramServiceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_AssignedTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TelegramServiceRequests_TelegramUsers_StatusUpdatedByTelegr~",
                table: "TelegramServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_TelegramServiceRequests_AssignedByTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_TelegramServiceRequests_AssignedTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_TelegramServiceRequests_StatusUpdatedByTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedByTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedTelegramUserId",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAt",
                table: "TelegramServiceRequests");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedByTelegramUserId",
                table: "TelegramServiceRequests");
        }
    }
}
