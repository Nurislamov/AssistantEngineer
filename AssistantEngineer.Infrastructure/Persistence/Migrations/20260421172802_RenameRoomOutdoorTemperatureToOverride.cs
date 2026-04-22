using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoomOutdoorTemperatureToOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutdoorTemperatureC",
                table: "Rooms",
                newName: "OutdoorTemperatureOverrideC");

            migrationBuilder.AlterColumn<double>(
                name: "OutdoorTemperatureOverrideC",
                table: "Rooms",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutdoorTemperatureOverrideC",
                table: "Rooms",
                newName: "OutdoorTemperatureC");

            migrationBuilder.AlterColumn<double>(
                name: "OutdoorTemperatureC",
                table: "Rooms",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);
        }
    }
}
