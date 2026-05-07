using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineBuildingHeatingResultAssembler
{
    private readonly EnergyCalculationPipelineDiagnosticsPolicy _diagnosticsPolicy;

    public EnergyCalculationPipelineBuildingHeatingResultAssembler(
        EnergyCalculationPipelineDiagnosticsPolicy diagnosticsPolicy)
    {
        _diagnosticsPolicy = diagnosticsPolicy;
    }

    public Result<IReadOnlyList<RoomHeatingLoadResult>> BuildRoomHeatingResults(
        Building building,
        CalculationPreferences preferences,
        HeatingLoadCalculationMethod method,
        string roomPipelineMethod,
        string designPointProfile,
        Func<Room, string?, Result<RoomLoadCalculationResult>> calculateRoomLoad)
    {
        var requestedMethod = method.ToString();
        var roomResults = new List<RoomHeatingLoadResult>();

        foreach (var room in building.Floors.SelectMany(floor => floor.Rooms).OrderBy(room => room.Id))
        {
            var roomLoad = calculateRoomLoad(room, requestedMethod);
            var roomFailure = _diagnosticsPolicy.TryMapRoomLoadFailureOrValidation<IReadOnlyList<RoomHeatingLoadResult>>(roomLoad);
            if (roomFailure is not null)
                return roomFailure;

            roomResults.Add(
                EnergyCalculationPipelineResultMapper.MapHeatingRoomResult(
                    room,
                    roomLoad.Value,
                    method,
                    preferences,
                    roomPipelineMethod,
                    designPointProfile));
        }

        return Result<IReadOnlyList<RoomHeatingLoadResult>>.Success(roomResults);
    }
}
