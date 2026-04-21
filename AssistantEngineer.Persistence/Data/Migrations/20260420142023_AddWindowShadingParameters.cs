using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowShadingParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ShadingDiffuseSolarShareUnaffected",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.3);

            migrationBuilder.AddColumn<double>(
                name: "ShadingMinimumDirectSolarReductionFactor",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.15);

            migrationBuilder.AddColumn<double>(
                name: "ShadingOverhangDepthM",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadingRevealDepthM",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadingSideFinDepthM",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadingWindowHeightM",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadingWindowWidthM",
                table: "Windows",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShadingDiffuseSolarShareUnaffected",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingMinimumDirectSolarReductionFactor",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingOverhangDepthM",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingRevealDepthM",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingSideFinDepthM",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingWindowHeightM",
                table: "Windows");

            migrationBuilder.DropColumn(
                name: "ShadingWindowWidthM",
                table: "Windows");
        }
    }
}
