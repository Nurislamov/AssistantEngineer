using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInfiltrationVentilationParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "InfiltrationAirChangesPerHour",
                table: "VentilationParameters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StackCoefficient",
                table: "VentilationParameters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WindCoefficient",
                table: "VentilationParameters",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WindExposureFactor",
                table: "VentilationParameters",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InfiltrationAirChangesPerHour",
                table: "VentilationParameters");

            migrationBuilder.DropColumn(
                name: "StackCoefficient",
                table: "VentilationParameters");

            migrationBuilder.DropColumn(
                name: "WindCoefficient",
                table: "VentilationParameters");

            migrationBuilder.DropColumn(
                name: "WindExposureFactor",
                table: "VentilationParameters");
        }
    }
}
