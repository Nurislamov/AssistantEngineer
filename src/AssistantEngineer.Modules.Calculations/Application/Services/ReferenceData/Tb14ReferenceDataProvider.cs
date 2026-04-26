using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class Tb14ReferenceDataProvider : ITb14ReferenceDataProvider
{
    private const string Table = "tb14";
    public string Version => "seed-v1";

    private static readonly IReadOnlyDictionary<RoomType, Tb14VentilationStandardRow> Rows =
        new Dictionary<RoomType, Tb14VentilationStandardRow>
        {
            [RoomType.Office] = new(
                Table,
                "seed-v1",
                RoomType.Office,
                10.0,
                0.35,
                0.0,
                true,
                "Seed ventilation row for office-type occupancy."),

            [RoomType.MeetingRoom] = new(
                Table,
                "seed-v1",
                RoomType.MeetingRoom,
                12.5,
                0.50,
                0.0,
                true,
                "Seed ventilation row for dense intermittently occupied rooms."),

            [RoomType.Corridor] = new(
                Table,
                "seed-v1",
                RoomType.Corridor,
                2.0,
                0.30,
                0.5,
                true,
                "Seed corridor ventilation/exhaust row."),

            [RoomType.ServerRoom] = new(
                Table,
                "seed-v1",
                RoomType.ServerRoom,
                0.2,
                0.30,
                2.0,
                false,
                "Seed server-room row with low occupancy air but mechanical exhaust expectation."),

            [RoomType.Retail] = new(
                Table,
                "seed-v1",
                RoomType.Retail,
                8.0,
                0.50,
                0.0,
                true,
                "Seed retail ventilation row."),

            [RoomType.Residential] = new(
                Table,
                "seed-v1",
                RoomType.Residential,
                7.0,
                0.30,
                0.5,
                true,
                "Seed residential airflow row. Replace with country-specific dwelling standard if needed."),

            [RoomType.Other] = new(
                Table,
                "seed-v1",
                RoomType.Other,
                7.5,
                0.35,
                0.0,
                true,
                "Fallback seed ventilation row for unsupported room categories.")
        };

    public Tb14VentilationStandardRow GetRow(RoomType roomType) =>
        Rows.TryGetValue(roomType, out var row)
            ? row
            : Rows[RoomType.Other];

    public IReadOnlyList<Tb14VentilationStandardRow> GetAll() =>
        Rows.Values.OrderBy(row => row.RoomType).ToArray();
}