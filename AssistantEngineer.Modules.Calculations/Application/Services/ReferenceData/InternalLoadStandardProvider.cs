using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class InternalLoadStandardProvider : IInternalLoadStandardProvider
{
    private const string Table = "internal-loads";
    public string Version => "seed-v1";

    private static readonly IReadOnlyDictionary<RoomType, InternalLoadStandardRow> Rows =
        new Dictionary<RoomType, InternalLoadStandardRow>
        {
            [RoomType.Office] = new(
                Table,
                "seed-v1",
                RoomType.Office,
                125,
                55,
                12,
                10,
                0.8,
                10,
                "Seed defaults. Replace with validated project standard table when available."),
                
            [RoomType.MeetingRoom] = new(
                Table,
                "seed-v1",
                RoomType.MeetingRoom,
                125,
                55,
                8,
                10,
                3.8,
                16,
                "Seed defaults for dense intermittently occupied spaces."),

            [RoomType.Corridor] = new(
                Table,
                "seed-v1",
                RoomType.Corridor,
                80,
                45,
                1,
                3,
                0.5,
                2,
                "Seed defaults for transit spaces with low internal gains."),

            [RoomType.ServerRoom] = new(
                Table,
                "seed-v1",
                RoomType.ServerRoom,
                125,
                20,
                120,
                5,
                0.5,
                1,
                "Seed defaults emphasizing equipment-driven heat gains."),

            [RoomType.Retail] = new(
                Table,
                "seed-v1",
                RoomType.Retail,
                170,
                65,
                15,
                15,
                4.5,
                12,
                "Seed defaults for publicly occupied commercial floor area."),

            [RoomType.Residential] = new(
                Table,
                "seed-v1",
                RoomType.Residential,
                80,
                55,
                4,
                5,
                0.5,
                4,
                "Seed defaults for apartments and dwelling-like zones."),

            [RoomType.Other] = new(
                Table,
                "seed-v1",
                RoomType.Other,
                125,
                50,
                10,
                10,
                0.8,
                8,
                "Fallback seed defaults for unsupported room categories.")
        };

    public InternalLoadStandardRow GetRow(RoomType roomType) =>
        Rows.TryGetValue(roomType, out var row)
            ? row
            : Rows[RoomType.Other];

    public IReadOnlyList<InternalLoadStandardRow> GetAll() =>
        Rows.Values.OrderBy(row => row.RoomType).ToArray();
}