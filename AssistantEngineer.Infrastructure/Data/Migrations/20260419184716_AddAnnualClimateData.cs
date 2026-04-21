using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualClimateData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData");

            migrationBuilder.AlterColumn<int>(
                name: "Hour",
                table: "HourlyClimateData",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ClimateDataId",
                table: "HourlyClimateData",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AnnualClimateDataId",
                table: "HourlyClimateData",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HourOfYear",
                table: "HourlyClimateData",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnnualClimateData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClimateZoneId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualClimateData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnualClimateData_ClimateZones_ClimateZoneId",
                        column: x => x.ClimateZoneId,
                        principalTable: "ClimateZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_AnnualClimateDataId_HourOfYear",
                table: "HourlyClimateData",
                columns: new[] { "AnnualClimateDataId", "HourOfYear" },
                unique: true,
                filter: "[AnnualClimateDataId] IS NOT NULL AND [HourOfYear] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData",
                columns: new[] { "ClimateDataId", "Hour" },
                unique: true,
                filter: "[ClimateDataId] IS NOT NULL AND [Hour] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AnnualClimateData_ClimateZoneId_Year",
                table: "AnnualClimateData",
                columns: new[] { "ClimateZoneId", "Year" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HourlyClimateData_AnnualClimateData_AnnualClimateDataId",
                table: "HourlyClimateData",
                column: "AnnualClimateDataId",
                principalTable: "AnnualClimateData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HourlyClimateData_AnnualClimateData_AnnualClimateDataId",
                table: "HourlyClimateData");

            migrationBuilder.DropTable(
                name: "AnnualClimateData");

            migrationBuilder.DropIndex(
                name: "IX_HourlyClimateData_AnnualClimateDataId_HourOfYear",
                table: "HourlyClimateData");

            migrationBuilder.DropIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "AnnualClimateDataId",
                table: "HourlyClimateData");

            migrationBuilder.DropColumn(
                name: "HourOfYear",
                table: "HourlyClimateData");

            migrationBuilder.AlterColumn<int>(
                name: "Hour",
                table: "HourlyClimateData",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClimateDataId",
                table: "HourlyClimateData",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HourlyClimateData_ClimateDataId_Hour",
                table: "HourlyClimateData",
                columns: new[] { "ClimateDataId", "Hour" },
                unique: true);
        }
    }
}
