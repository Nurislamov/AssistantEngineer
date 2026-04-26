using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Floors_BuildingId",
                table: "Floors");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_ProjectId",
                table: "Buildings");

            migrationBuilder.AlterColumn<string>(
                name: "Orientation",
                table: "Windows",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Orientation",
                table: "Walls",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Rooms",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FloorId_Name",
                table: "Rooms",
                columns: new[] { "FloorId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId_Name",
                table: "Floors",
                columns: new[] { "BuildingId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCatalogItems_CatalogIdentity",
                table: "EquipmentCatalogItems",
                columns: new[] { "Manufacturer", "SystemType", "UnitType", "ModelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_ProjectId_Name",
                table: "Buildings",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_FloorId_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Name",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Floors_BuildingId_Name",
                table: "Floors");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentCatalogItems_CatalogIdentity",
                table: "EquipmentCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_ProjectId_Name",
                table: "Buildings");

            migrationBuilder.AlterColumn<int>(
                name: "Orientation",
                table: "Windows",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Orientation",
                table: "Walls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Rooms",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId",
                table: "Floors",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_ProjectId",
                table: "Buildings",
                column: "ProjectId");
        }
    }
}
