using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClimateDataEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClimateData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimateZoneId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    DailyTemperatureRange = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClimateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClimateData_ClimateZones_ClimateZoneId",
                        column: x => x.ClimateZoneId,
                        principalTable: "ClimateZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HourlyClimateData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimateDataId = table.Column<int>(type: "integer", nullable: false),
                    Hour = table.Column<int>(type: "integer", nullable: false),
                    DryBulbTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DirectSolarRadiation = table.Column<double>(type: "double precision", nullable: false),
                    DiffuseSolarRadiation = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyClimateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyClimateData_ClimateData_ClimateDataId",
                        column: x => x.ClimateDataId,
                        principalTable: "ClimateData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClimateData_ClimateZoneId_Month",
                table: "ClimateData",
                columns: new[] { "ClimateZoneId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData",
                columns: new[] { "ClimateDataId", "Hour" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HourlyClimateData");

            migrationBuilder.DropTable(
                name: "ClimateData");
        }
    }
}
