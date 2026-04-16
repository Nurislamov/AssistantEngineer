using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignCapacityToStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityKw",
                table: "Rooms",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityW",
                table: "Rooms",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReserveFactor",
                table: "Rooms",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityKw",
                table: "Floors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityW",
                table: "Floors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReserveFactor",
                table: "Floors",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityKw",
                table: "Buildings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DesignCapacityW",
                table: "Buildings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ReserveFactor",
                table: "Buildings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesignCapacityKw",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "DesignCapacityW",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ReserveFactor",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "DesignCapacityKw",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "DesignCapacityW",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "ReserveFactor",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "DesignCapacityKw",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "DesignCapacityW",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "ReserveFactor",
                table: "Buildings");
        }
    }
}
