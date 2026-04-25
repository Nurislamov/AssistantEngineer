using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationPreviewService
{
    private readonly IRoomRepository _rooms;
    private readonly INaturalVentilationOpeningControlService _openingControl;
    private readonly INaturalVentilationAirflowService _airflow;
    private readonly Iso52016EnergyNeedOptions _energyOptions;

    public NaturalVentilationPreviewService(
        IRoomRepository rooms,
        INaturalVentilationOpeningControlService openingControl,
        INaturalVentilationAirflowService airflow,
        IOptions<Iso52016EnergyNeedOptions> energyOptions)
    {
        _rooms = rooms;
        _openingControl = openingControl;
        _airflow = airflow;
        _energyOptions = energyOptions.Value;
    }

    public async Task<Result<NaturalVentilationPreviewResponse>> PreviewAsync(
        int roomId,
        NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.HourOfDay is < 0 or > 23)
            return Result<NaturalVentilationPreviewResponse>.Validation("HourOfDay must be between 0 and 23.");

        if (request.DemandFactor is < 0 or > 1)
            return Result<NaturalVentilationPreviewResponse>.Validation("DemandFactor must be between 0 and 1.");

        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
            return Result<NaturalVentilationPreviewResponse>.NotFound($"Room with id {roomId} not found.");

        var opening = _openingControl.Resolve(
            room,
            request.IndoorTemperatureC,
            request.OutdoorTemperatureC,
            request.WindSpeedMPerS,
            request.DemandFactor,
            request.HourOfDay);

        var heatTransfer = _airflow.CalculateHeatTransferCoefficient(
            room,
            request.IndoorTemperatureC,
            request.OutdoorTemperatureC,
            request.WindSpeedMPerS,
            request.DemandFactor,
            request.HourOfDay);

        var roomVolume = Math.Max(room.CalculateVolume(), 0.001);
        var equivalentAch = heatTransfer / (_energyOptions.AirHeatCapacityWhPerM3K * roomVolume);

        return Result<NaturalVentilationPreviewResponse>.Success(new NaturalVentilationPreviewResponse
        {
            RoomId = room.Id,
            RoomName = room.Name,
            IndoorTemperatureC = request.IndoorTemperatureC,
            OutdoorTemperatureC = request.OutdoorTemperatureC,
            WindSpeedMPerS = request.WindSpeedMPerS,
            DemandFactor = request.DemandFactor,
            HourOfDay = request.HourOfDay,
            IsOpen = opening.IsOpen,
            OpeningFactor = Math.Round(opening.OpeningFactor, 4),
            EffectiveOpeningAreaM2 = Math.Round(opening.EffectiveOpeningAreaM2, 4),
            Reason = opening.Reason,
            HeatTransferCoefficientWPerK = Math.Round(heatTransfer, 4),
            EquivalentAirChangesPerHour = Math.Round(equivalentAch, 4)
        });
    }
}