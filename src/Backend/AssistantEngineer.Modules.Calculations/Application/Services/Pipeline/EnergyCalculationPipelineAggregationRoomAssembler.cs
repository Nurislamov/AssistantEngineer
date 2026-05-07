using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineAggregationRoomAssembler
{
    public Result<IReadOnlyList<AggregationRoomLoadInput>> BuildAggregationRooms(
        IEnumerable<Room> sourceRooms,
        Building building,
        Func<Room, Result<RoomLoadCalculationResult>> calculateRoomLoad)
    {
        var roomToZone = building.ThermalZones
            .SelectMany(zone => zone.AssignedRooms.Select(room => new { room.Id, ZoneId = (int?)zone.Id }))
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First().ZoneId);
        var rooms = new List<AggregationRoomLoadInput>();

        foreach (var room in sourceRooms.OrderBy(room => room.Id))
        {
            var load = calculateRoomLoad(room);
            if (load.IsFailure)
                return Result<IReadOnlyList<AggregationRoomLoadInput>>.Failure(load);

            if (load.Value.HasErrors)
                return Result<IReadOnlyList<AggregationRoomLoadInput>>.Validation(FormatErrorDiagnostics(load.Value.Diagnostics));

            rooms.Add(new AggregationRoomLoadInput(
                RoomId: room.Id,
                RoomName: room.Name,
                ThermalZoneId: roomToZone.GetValueOrDefault(room.Id),
                FloorId: room.FloorId,
                BuildingId: building.Id,
                AreaM2: room.Area.SquareMeters,
                HeatingLoadW: load.Value.HeatingLoadW,
                CoolingLoadW: load.Value.CoolingLoadW,
                HeatingBreakdown: load.Value.HeatingBreakdown,
                CoolingBreakdown: load.Value.CoolingBreakdown,
                HourlyHeatingLoadW: Enumerable.Repeat(load.Value.HeatingLoadW, 24).ToArray(),
                HourlyCoolingLoadW: Enumerable.Repeat(load.Value.CoolingLoadW, 24).ToArray()));
        }

        return Result<IReadOnlyList<AggregationRoomLoadInput>>.Success(rooms);
    }

    private static string FormatErrorDiagnostics(IEnumerable<CalculationDiagnostic> diagnostics) =>
        string.Join("; ", diagnostics
            .Where(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
}
