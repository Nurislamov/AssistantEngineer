using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitHourlyClimateData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualHourlyData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnualClimateDataId = table.Column<int>(type: "integer", nullable: false),
                    HourOfYear = table.Column<int>(type: "integer", nullable: false),
                    DryBulbTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DirectSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    DiffuseSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    RelativeHumidityPercent = table.Column<double>(type: "double precision", nullable: true),
                    AtmosphericPressurePa = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedMPerS = table.Column<double>(type: "double precision", nullable: true),
                    WindDirectionDegrees = table.Column<double>(type: "double precision", nullable: true),
                    HorizontalInfraredRadiationWPerM2 = table.Column<double>(type: "double precision", nullable: true),
                    SkyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    TotalSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true),
                    OpaqueSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualHourlyData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnualHourlyData_AnnualClimateData_AnnualClimateDataId",
                        column: x => x.AnnualClimateDataId,
                        principalTable: "AnnualClimateData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DesignDayHourlyData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimateDataId = table.Column<int>(type: "integer", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    DryBulbTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DirectSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    DiffuseSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    RelativeHumidityPercent = table.Column<double>(type: "double precision", nullable: true),
                    AtmosphericPressurePa = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedMPerS = table.Column<double>(type: "double precision", nullable: true),
                    WindDirectionDegrees = table.Column<double>(type: "double precision", nullable: true),
                    HorizontalInfraredRadiationWPerM2 = table.Column<double>(type: "double precision", nullable: true),
                    SkyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    TotalSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true),
                    OpaqueSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignDayHourlyData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignDayHourlyData_ClimateData_ClimateDataId",
                        column: x => x.ClimateDataId,
                        principalTable: "ClimateData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualHourlyData_AnnualClimateDataId_HourOfYear",
                table: "AnnualHourlyData",
                columns: new[] { "AnnualClimateDataId", "HourOfYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignDayHourlyData_ClimateDataId_Hour",
                table: "DesignDayHourlyData",
                columns: new[] { "ClimateDataId", "Hour" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "DesignDayHourlyData" (
                    "Id",
                    "ClimateDataId",
                    "Hour",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths")
                SELECT
                    "Id",
                    "ClimateDataId",
                    "Hour",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths"
                FROM "HourlyClimateData"
                WHERE "ClimateDataId" IS NOT NULL
                  AND "Hour" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "AnnualHourlyData" (
                    "Id",
                    "AnnualClimateDataId",
                    "HourOfYear",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths")
                SELECT
                    "Id",
                    "AnnualClimateDataId",
                    "HourOfYear",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths"
                FROM "HourlyClimateData"
                WHERE "AnnualClimateDataId" IS NOT NULL
                  AND "HourOfYear" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('"DesignDayHourlyData"', 'Id'),
                    GREATEST(COALESCE(MAX("Id"), 1), 1),
                    COALESCE(MAX("Id"), 0) > 0)
                FROM "DesignDayHourlyData";
                """);

            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('"AnnualHourlyData"', 'Id'),
                    GREATEST(COALESCE(MAX("Id"), 1), 1),
                    COALESCE(MAX("Id"), 0) > 0)
                FROM "AnnualHourlyData";
                """);

            migrationBuilder.DropTable(
                name: "HourlyClimateData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HourlyClimateData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnualClimateDataId = table.Column<int>(type: "integer", nullable: true),
                    ClimateDataId = table.Column<int>(type: "integer", nullable: true),
                    AtmosphericPressurePa = table.Column<double>(type: "double precision", nullable: true),
                    DiffuseSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    DirectSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    DryBulbTemperature = table.Column<double>(type: "double precision", nullable: false),
                    HorizontalInfraredRadiationWPerM2 = table.Column<double>(type: "double precision", nullable: true),
                    Hour = table.Column<int>(type: "integer", nullable: true),
                    HourOfYear = table.Column<int>(type: "integer", nullable: true),
                    OpaqueSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true),
                    RelativeHumidityPercent = table.Column<double>(type: "double precision", nullable: true),
                    SkyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    TotalSkyCoverTenths = table.Column<double>(type: "double precision", nullable: true),
                    WindDirectionDegrees = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedMPerS = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyClimateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyClimateData_AnnualClimateData_AnnualClimateDataId",
                        column: x => x.AnnualClimateDataId,
                        principalTable: "AnnualClimateData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HourlyClimateData_ClimateData_ClimateDataId",
                        column: x => x.ClimateDataId,
                        principalTable: "ClimateData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_AnnualClimateDataId_HourOfYear",
                table: "HourlyClimateData",
                columns: new[] { "AnnualClimateDataId", "HourOfYear" },
                unique: true,
                filter: "\"AnnualClimateDataId\" IS NOT NULL AND \"HourOfYear\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData",
                columns: new[] { "ClimateDataId", "Hour" },
                unique: true,
                filter: "\"ClimateDataId\" IS NOT NULL AND \"Hour\" IS NOT NULL");

            migrationBuilder.Sql("""
                INSERT INTO "HourlyClimateData" (
                    "ClimateDataId",
                    "Hour",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths")
                SELECT
                    "ClimateDataId",
                    "Hour",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths"
                FROM "DesignDayHourlyData";
                """);

            migrationBuilder.Sql("""
                INSERT INTO "HourlyClimateData" (
                    "AnnualClimateDataId",
                    "HourOfYear",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths")
                SELECT
                    "AnnualClimateDataId",
                    "HourOfYear",
                    "DryBulbTemperature",
                    "DirectSolarRadiation",
                    "DiffuseSolarRadiation",
                    "RelativeHumidityPercent",
                    "AtmosphericPressurePa",
                    "WindSpeedMPerS",
                    "WindDirectionDegrees",
                    "HorizontalInfraredRadiationWPerM2",
                    "SkyTemperatureC",
                    "TotalSkyCoverTenths",
                    "OpaqueSkyCoverTenths"
                FROM "AnnualHourlyData";
                """);

            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('"HourlyClimateData"', 'Id'),
                    GREATEST(COALESCE(MAX("Id"), 1), 1),
                    COALESCE(MAX("Id"), 0) > 0)
                FROM "HourlyClimateData";
                """);

            migrationBuilder.DropTable(
                name: "AnnualHourlyData");

            migrationBuilder.DropTable(
                name: "DesignDayHourlyData");
        }
    }
}
