using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class DomesticHotWaterStandardProvider : IDomesticHotWaterStandardProvider
{
    private const string Table = "dhw";
    public string Version => "seed-v1";

    private static readonly IReadOnlyDictionary<RoomType, DomesticHotWaterStandardRow> Rows =
        new Dictionary<RoomType, DomesticHotWaterStandardRow>
        {
            [RoomType.Office] = new(
                Table,
                "seed-v1",
                RoomType.Office,
                5,
                10,
                60,
                0.05,
                0.0,
                0.0,
                "Seed office default with low sanitary hot-water intensity."),

            [RoomType.MeetingRoom] = new(
                Table,
                "seed-v1",
                RoomType.MeetingRoom,
                3,
                10,
                60,
                0.05,
                0.0,
                0.0,
                "Seed meeting-space default. Replace with project-specific sanitary assumptions if needed."),

            [RoomType.Corridor] = new(
                Table,
                "seed-v1",
                RoomType.Corridor,
                0.5,
                10,
                60,
                0.05,
                0.0,
                0.0,
                "Seed transient-space DHW intensity."),

            [RoomType.ServerRoom] = new(
                Table,
                "seed-v1",
                RoomType.ServerRoom,
                0.0,
                10,
                60,
                0.00,
                0.0,
                0.0,
                "Seed default for spaces without regular sanitary hot-water demand."),

            [RoomType.Retail] = new(
                Table,
                "seed-v1",
                RoomType.Retail,
                3.5,
                10,
                60,
                0.05,
                0.1,
                0.0,
                "Seed retail default for staff/customer sanitary use."),

            [RoomType.Residential] = new(
                Table,
                "seed-v1",
                RoomType.Residential,
                40,
                10,
                60,
                0.10,
                1.0,
                0.5,
                "Seed residential default aligned with simple daily per-person DHW use assumptions."),

            [RoomType.Other] = new(
                Table,
                "seed-v1",
                RoomType.Other,
                8,
                10,
                60,
                0.05,
                0.0,
                0.0,
                "Fallback seed DHW defaults for unsupported room categories.")
        };

    public DomesticHotWaterStandardRow GetRow(RoomType roomType) =>
        Rows.TryGetValue(roomType, out var row)
            ? row
            : Rows[RoomType.Other];

    public IReadOnlyList<DomesticHotWaterStandardRow> GetAll() =>
        Rows.Values.OrderBy(row => row.RoomType).ToArray();
}