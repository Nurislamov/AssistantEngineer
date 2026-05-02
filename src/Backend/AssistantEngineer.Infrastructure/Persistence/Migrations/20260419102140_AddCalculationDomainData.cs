using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationDomainData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConstructionAssemblyId",
                table: "Walls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentScheduleId",
                table: "Rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LightingScheduleId",
                table: "Rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupancyScheduleId",
                table: "Rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VentilationParametersId",
                table: "Rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConstructionAssemblies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionAssemblies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HourlySchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Factors = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlySchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ThermalConductivityWPerMK = table.Column<double>(type: "double precision", nullable: false),
                    DensityKgPerM3 = table.Column<double>(type: "double precision", nullable: false),
                    SpecificHeatJPerKgK = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VentilationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AirChangesPerHour = table.Column<double>(type: "double precision", nullable: false),
                    HeatRecoveryEfficiency = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentilationParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConstructionLayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConstructionAssemblyId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    ThicknessM = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructionLayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructionLayers_ConstructionAssemblies_ConstructionAssem~",
                        column: x => x.ConstructionAssemblyId,
                        principalTable: "ConstructionAssemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConstructionLayers_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Walls_ConstructionAssemblyId",
                table: "Walls",
                column: "ConstructionAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_EquipmentScheduleId",
                table: "Rooms",
                column: "EquipmentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_LightingScheduleId",
                table: "Rooms",
                column: "LightingScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_OccupancyScheduleId",
                table: "Rooms",
                column: "OccupancyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_VentilationParametersId",
                table: "Rooms",
                column: "VentilationParametersId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionLayers_ConstructionAssemblyId",
                table: "ConstructionLayers",
                column: "ConstructionAssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructionLayers_MaterialId",
                table: "ConstructionLayers",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_HourlySchedules_EquipmentScheduleId",
                table: "Rooms",
                column: "EquipmentScheduleId",
                principalTable: "HourlySchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_HourlySchedules_LightingScheduleId",
                table: "Rooms",
                column: "LightingScheduleId",
                principalTable: "HourlySchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_HourlySchedules_OccupancyScheduleId",
                table: "Rooms",
                column: "OccupancyScheduleId",
                principalTable: "HourlySchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_VentilationParameters_VentilationParametersId",
                table: "Rooms",
                column: "VentilationParametersId",
                principalTable: "VentilationParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Walls_ConstructionAssemblies_ConstructionAssemblyId",
                table: "Walls",
                column: "ConstructionAssemblyId",
                principalTable: "ConstructionAssemblies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_HourlySchedules_EquipmentScheduleId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_HourlySchedules_LightingScheduleId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_HourlySchedules_OccupancyScheduleId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_VentilationParameters_VentilationParametersId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Walls_ConstructionAssemblies_ConstructionAssemblyId",
                table: "Walls");

            migrationBuilder.DropTable(
                name: "ConstructionLayers");

            migrationBuilder.DropTable(
                name: "HourlySchedules");

            migrationBuilder.DropTable(
                name: "VentilationParameters");

            migrationBuilder.DropTable(
                name: "ConstructionAssemblies");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Walls_ConstructionAssemblyId",
                table: "Walls");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_EquipmentScheduleId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_LightingScheduleId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_OccupancyScheduleId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_VentilationParametersId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ConstructionAssemblyId",
                table: "Walls");

            migrationBuilder.DropColumn(
                name: "EquipmentScheduleId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "LightingScheduleId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "OccupancyScheduleId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "VentilationParametersId",
                table: "Rooms");
        }
    }
}
