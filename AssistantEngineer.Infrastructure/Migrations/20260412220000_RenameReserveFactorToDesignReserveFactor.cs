using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Migrations
{
    /// <inheritdoc />
    public partial class RenameReserveFactorToDesignReserveFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReserveFactor",
                table: "Rooms",
                newName: "DesignReserveFactor");

            migrationBuilder.RenameColumn(
                name: "ReserveFactor",
                table: "Floors",
                newName: "DesignReserveFactor");

            migrationBuilder.RenameColumn(
                name: "ReserveFactor",
                table: "Buildings",
                newName: "DesignReserveFactor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DesignReserveFactor",
                table: "Rooms",
                newName: "ReserveFactor");

            migrationBuilder.RenameColumn(
                name: "DesignReserveFactor",
                table: "Floors",
                newName: "ReserveFactor");

            migrationBuilder.RenameColumn(
                name: "DesignReserveFactor",
                table: "Buildings",
                newName: "ReserveFactor");
        }
    }
}
