using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistantEngineer.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceThermalZoneRoomIdsJsonbWithJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThermalZoneRooms",
                columns: table => new
                {
                    ThermalZoneId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThermalZoneRooms", x => new { x.ThermalZoneId, x.RoomId });
                    table.ForeignKey(
                        name: "FK_ThermalZoneRooms_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThermalZoneRooms_ThermalZones_ThermalZoneId",
                        column: x => x.ThermalZoneId,
                        principalTable: "ThermalZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThermalZoneRooms_RoomId",
                table: "ThermalZoneRooms",
                column: "RoomId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "ThermalZoneRooms" ("ThermalZoneId", "RoomId")
                SELECT DISTINCT zone."Id", room_id.value::integer
                FROM "ThermalZones" zone
                CROSS JOIN LATERAL jsonb_array_elements_text(zone."RoomIds") AS room_id(value)
                INNER JOIN "Rooms" room ON room."Id" = room_id.value::integer;
                """);

            migrationBuilder.DropColumn(
                name: "RoomIds",
                table: "ThermalZones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomIds",
                table: "ThermalZones",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE "ThermalZones" zone
                SET "RoomIds" = COALESCE(room_ids."RoomIds", '[]'::jsonb)
                FROM (
                    SELECT
                        "ThermalZoneId",
                        jsonb_agg("RoomId" ORDER BY "RoomId") AS "RoomIds"
                    FROM "ThermalZoneRooms"
                    GROUP BY "ThermalZoneId"
                ) room_ids
                WHERE zone."Id" = room_ids."ThermalZoneId";
                """);

            migrationBuilder.DropTable(
                name: "ThermalZoneRooms");
        }
    }
}
