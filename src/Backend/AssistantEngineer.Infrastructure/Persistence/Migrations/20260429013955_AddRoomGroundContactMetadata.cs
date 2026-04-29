using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomGroundContactMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjacentRoomId",
                table: "Walls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoundaryType",
                table: "Walls",
                type: "text",
                nullable: false,
                defaultValue: "External");

            migrationBuilder.Sql(
                "UPDATE \"Walls\" SET \"BoundaryType\" = CASE WHEN \"IsExternal\" THEN 'External' ELSE 'Adiabatic' END");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ThermalZones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<double>(
                name: "GroundBurialDepthM",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroundContactType",
                table: "Rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GroundExposedPerimeterM",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GroundHorizontalInsulationWidthM",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GroundPerimeterInsulationDepthM",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GroundUnderfloorVentilationAirChangesPerHour",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GroundWallHeightBelowGradeM",
                table: "Rooms",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Walls_AdjacentRoomId",
                table: "Walls",
                column: "AdjacentRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Walls_Rooms_AdjacentRoomId",
                table: "Walls",
                column: "AdjacentRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Walls_Rooms_AdjacentRoomId",
                table: "Walls");

            migrationBuilder.DropIndex(
                name: "IX_Walls_AdjacentRoomId",
                table: "Walls");

            migrationBuilder.DropColumn(
                name: "AdjacentRoomId",
                table: "Walls");

            migrationBuilder.DropColumn(
                name: "BoundaryType",
                table: "Walls");

            migrationBuilder.DropColumn(
                name: "GroundBurialDepthM",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundContactType",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundExposedPerimeterM",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundHorizontalInsulationWidthM",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundPerimeterInsulationDepthM",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundUnderfloorVentilationAirChangesPerHour",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GroundWallHeightBelowGradeM",
                table: "Rooms");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ThermalZones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
