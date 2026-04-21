using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpandedHourlyWeatherData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AtmosphericPressurePa",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HorizontalInfraredRadiationWPerM2",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OpaqueSkyCoverTenths",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RelativeHumidityPercent",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SkyTemperatureC",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalSkyCoverTenths",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindDirectionDegrees",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindSpeedMPerS",
                table: "HourlyClimateData",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtmosphericPressurePa",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "HorizontalInfraredRadiationWPerM2",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "OpaqueSkyCoverTenths",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "RelativeHumidityPercent",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "SkyTemperatureC",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "TotalSkyCoverTenths",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "WindDirectionDegrees",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "WindSpeedMPerS",
                table: "HourlyClimateData");
        }
    }
}
