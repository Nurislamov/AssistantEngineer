using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public sealed class RoomVentilationDefaultsService
{
    private readonly IRoomRepository _rooms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoomVentilationDefaultsProvider _defaultsProvider;
    private readonly ILogger<RoomVentilationDefaultsService> _logger;
    public RoomVentilationDefaultsService(
        IRoomRepository rooms,
        IUnitOfWork unitOfWork,
        IRoomVentilationDefaultsProvider defaultsProvider,
        ILogger<RoomVentilationDefaultsService>? logger = null)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
        _defaultsProvider = defaultsProvider;
        _logger = logger ?? NullLogger<RoomVentilationDefaultsService>.Instance;
    }

    public async Task<Result<RoomVentilationDefaultsResponse>> PreviewAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetWithVentilationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomVentilationDefaultsResponse>.NotFound($"Room with id {roomId} not found.");

        var defaults = _defaultsProvider.GetDefaults(room);

        return Result<RoomVentilationDefaultsResponse>.Success(new RoomVentilationDefaultsResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            HasExistingVentilationParameters = room.VentilationParameters is not null,
            CanApply = defaults.CanApply,
            Reason = defaults.Reason,
            DesignPeopleCount = defaults.DesignPeopleCount,
            DesignOutdoorAirLitersPerSecond = defaults.DesignOutdoorAirLitersPerSecond,
            OutdoorAirAirChangesPerHour = defaults.OutdoorAirAirChangesPerHour,
            ExhaustAirChangesPerHour = defaults.ExhaustAirChangesPerHour,
            ProposedAirChangesPerHour = defaults.ProposedAirChangesPerHour,
            HeatRecoveryEfficiency = defaults.HeatRecoveryEfficiency,
            InfiltrationAirChangesPerHour = defaults.InfiltrationAirChangesPerHour,
            WindExposureFactor = defaults.WindExposureFactor,
            StackCoefficient = defaults.StackCoefficient,
            WindCoefficient = defaults.WindCoefficient,
            SourceTableVersion = defaults.SourceTableVersion
        });
    }

    public async Task<Result<RoomVentilationParametersResponse>> ApplyAsync(
        int roomId,
        ApplyRoomVentilationDefaultsRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetWithVentilationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomVentilationParametersResponse>.NotFound($"Room with id {roomId} not found.");

        var defaults = _defaultsProvider.GetDefaults(room);
        if (!defaults.CanApply)
            return Result<RoomVentilationParametersResponse>.Validation(
                string.IsNullOrWhiteSpace(defaults.Reason)
                    ? "Ventilation defaults cannot be applied for this room."
                    : defaults.Reason);

        if (room.VentilationParameters is not null && !request.OverwriteExistingParameters)
        {
            return Result<RoomVentilationParametersResponse>.Validation(
                "Room already has ventilation parameters. Set OverwriteExistingParameters = true to replace them.");
        }

        if (room.VentilationParameters is null)
        {
            var createResult = VentilationParameters.Create(
                defaults.ProposedAirChangesPerHour,
                defaults.HeatRecoveryEfficiency,
                defaults.InfiltrationAirChangesPerHour,
                defaults.WindExposureFactor,
                defaults.StackCoefficient,
                defaults.WindCoefficient);

            if (createResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(createResult);

            var setResult = room.SetVentilationParameters(createResult.Value);
            if (setResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(setResult);
        }
        else
        {
            var updateResult = room.VentilationParameters.Update(
                defaults.ProposedAirChangesPerHour,
                defaults.HeatRecoveryEfficiency,
                defaults.InfiltrationAirChangesPerHour,
                defaults.WindExposureFactor,
                defaults.StackCoefficient,
                defaults.WindCoefficient);

            if (updateResult.IsFailure)
                return Result<RoomVentilationParametersResponse>.Failure(updateResult);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Applied TB14-derived ventilation defaults to room {RoomId}. OverwriteExistingParameters={OverwriteExistingParameters}",
            roomId,
            request.OverwriteExistingParameters);

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
}