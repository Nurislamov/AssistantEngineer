using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public sealed class RoomVentilationQueryService
{
    private readonly IRoomRepository _rooms;

    public RoomVentilationQueryService(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<Result<RoomVentilationParametersResponse>> GetAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetWithVentilationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomVentilationParametersResponse>.NotFound($"Room with id {roomId} not found.");

        if (room.VentilationParameters is null)
            return Result<RoomVentilationParametersResponse>.NotFound($"Ventilation parameters for room {roomId} not found.");

        return Result<RoomVentilationParametersResponse>.Success(new RoomVentilationParametersResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            AirChangesPerHour = room.VentilationParameters.AirChangesPerHour,
            HeatRecoveryEfficiency = room.VentilationParameters.HeatRecoveryEfficiency,
            InfiltrationAirChangesPerHour = room.VentilationParameters.InfiltrationAirChangesPerHour,
            WindExposureFactor = room.VentilationParameters.WindExposureFactor,
            StackCoefficient = room.VentilationParameters.StackCoefficient,
            WindCoefficient = room.VentilationParameters.WindCoefficient
        });
    }
}