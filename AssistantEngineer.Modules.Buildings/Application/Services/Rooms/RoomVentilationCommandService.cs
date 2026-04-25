using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public sealed class RoomVentilationCommandService
{
    private readonly IRoomRepository _rooms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomVentilationCommandService> _logger;

    public RoomVentilationCommandService(
        IRoomRepository rooms,
        IUnitOfWork unitOfWork,
        ILogger<RoomVentilationCommandService>? logger = null)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<RoomVentilationCommandService>.Instance;
    }

    public async Task<Result<RoomVentilationParametersResponse>> UpsertAsync(
        int roomId,
        UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetWithVentilationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomVentilationParametersResponse>.NotFound($"Room with id {roomId} not found.");

        if (room.VentilationParameters is null)
        {
            var createResult = VentilationParameters.Create(
                request.AirChangesPerHour,
                request.HeatRecoveryEfficiency,
                request.InfiltrationAirChangesPerHour,
                request.WindExposureFactor,
                request.StackCoefficient,
                request.WindCoefficient);

            if (createResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(createResult);

            var setResult = room.SetVentilationParameters(createResult.Value);
            if (setResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(setResult);
        }
        else
        {
            var updateResult = room.VentilationParameters.Update(
                request.AirChangesPerHour,
                request.HeatRecoveryEfficiency,
                request.InfiltrationAirChangesPerHour,
                request.WindExposureFactor,
                request.StackCoefficient,
                request.WindCoefficient);

            if (updateResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(updateResult);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Upserted ventilation parameters for room {RoomId}.", roomId);

        return Result<RoomVentilationParametersResponse>.Success(new RoomVentilationParametersResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            AirChangesPerHour = room.VentilationParameters!.AirChangesPerHour,
            HeatRecoveryEfficiency = room.VentilationParameters.HeatRecoveryEfficiency,
            InfiltrationAirChangesPerHour = room.VentilationParameters.InfiltrationAirChangesPerHour,
            WindExposureFactor = room.VentilationParameters.WindExposureFactor,
            StackCoefficient = room.VentilationParameters.StackCoefficient,
            WindCoefficient = room.VentilationParameters.WindCoefficient
        });
    }

    public async Task<Result> DeleteAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetWithVentilationAsync(roomId, cancellationToken);
        if (room is null)
            return Result.NotFound($"Room with id {roomId} not found.");

        var result = room.SetVentilationParameters(null);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted ventilation parameters for room {RoomId}.", roomId);
        return Result.Success();
    }
}