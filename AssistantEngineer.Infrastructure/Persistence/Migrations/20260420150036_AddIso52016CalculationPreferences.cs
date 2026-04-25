using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIso52016CalculationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Iso52016DefaultAirChangesPerHour",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "Iso52016DiffuseSolarShareUnaffectedByShading",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.29999999999999999);

            migrationBuilder.AddColumn<double>(
                name: "Iso52016DirectSolarShadingReductionFactor",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "Iso52016InternalHeatCapacityJPerM2K",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 10000.0);

            migrationBuilder.AddColumn<double>(
                name: "Iso52016SolarUtilizationFactor",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.75);

            migrationBuilder.AddColumn<double>(
                name: "Iso52016WindowFrameAreaFraction",
                table: "CalculationPreferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.25);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Iso52016DefaultAirChangesPerHour",
                table: "CalculationPreferences");

            migrationBuilder.DropColumn(
                name: "Iso52016DiffuseSolarShareUnaffectedByShading",
                table: "CalculationPreferences");

            migrationBuilder.DropColumn(
                name: "Iso52016DirectSolarShadingReductionFactor",
                table: "CalculationPreferences");

            migrationBuilder.DropColumn(
                name: "Iso52016InternalHeatCapacityJPerM2K",
                table: "CalculationPreferences");

            migrationBuilder.DropColumn(
                name: "Iso52016SolarUtilizationFactor",
                table: "CalculationPreferences");

            migrationBuilder.DropColumn(
                name: "Iso52016WindowFrameAreaFraction",
                table: "CalculationPreferences");
        }
    }
}
