using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "Rooms",
                newName: "VolumeM3");

            migrationBuilder.RenameColumn(
                name: "OutdoorTemperature",
                table: "Rooms",
                newName: "OutdoorTemperatureC");

            migrationBuilder.RenameColumn(
                name: "IndoorTemperature",
                table: "Rooms",
                newName: "IndoorTemperatureC");

            migrationBuilder.RenameColumn(
                name: "Height",
                table: "Rooms",
                newName: "HeightM");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "Rooms",
                newName: "AreaM2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VolumeM3",
                table: "Rooms",
                newName: "Volume");

            migrationBuilder.RenameColumn(
                name: "OutdoorTemperatureC",
                table: "Rooms",
                newName: "OutdoorTemperature");

            migrationBuilder.RenameColumn(
                name: "IndoorTemperatureC",
                table: "Rooms",
                newName: "IndoorTemperature");

            migrationBuilder.RenameColumn(
                name: "HeightM",
                table: "Rooms",
                newName: "Height");

            migrationBuilder.RenameColumn(
                name: "AreaM2",
                table: "Rooms",
                newName: "Area");
        }
    }
}
