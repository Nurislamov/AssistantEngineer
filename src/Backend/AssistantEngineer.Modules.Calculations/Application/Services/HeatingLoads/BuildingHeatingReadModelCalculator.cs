using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;

public sealed class BuildingHeatingReadModelCalculator
{
    private const double InternalSurfaceResistance = 0.13;
    private const double ExternalSurfaceResistance = 0.04;

    private readonly En12831HeatingLoadOptions _options;
    private readonly ILogger<BuildingHeatingReadModelCalculator> _logger;

    public BuildingHeatingReadModelCalculator(
        IOptions<En12831HeatingLoadOptions> options,
        ILogger<BuildingHeatingReadModelCalculator>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? NullLogger<BuildingHeatingReadModelCalculator>.Instance;
    }

    public async Task<BuildingHeatingLoadResult> CalculateAsync(
        BuildingHeatingReadModel building,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        if (method != HeatingLoadCalculationMethod.En12831)
        {
            _logger.LogWarning(
                "Unsupported heating load calculation method {CalculationMethod} for building {BuildingId}.",
                method,
                building.BuildingId);
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported heating load method.");
        }

        _logger.LogDebug("Read-model EN 12831 heating calculation started for building {BuildingId}.", building.BuildingId);

        var rooms = new List<RoomHeatingLoadResult>(building.Rooms.Count);
        foreach (var room in building.Rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rooms.Add(await CalculateRoomAsync(
                room,
                building.WinterDesignTemperatureC,
                method,
                cancellationToken));
        }

        var transmissionLoss = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilationLoss = rooms.Sum(room => room.VentilationHeatLossW);
        var totalLoad = transmissionLoss + ventilationLoss;

        return new BuildingHeatingLoadResult
        {
            BuildingId = building.BuildingId,
            ProjectName = building.ProjectName,
            BuildingName = building.BuildingName,
            CalculationMethod = method.ToString(),
            RoomsCount = rooms.Count,
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0),
            Rooms = rooms
        };
    }

    private Task<RoomHeatingLoadResult> CalculateRoomAsync(
        RoomHeatingReadModel room,
        double? buildingWinterDesignTemperatureC,
        HeatingLoadCalculationMethod method,
        CancellationToken cancellationToken)
    {
        if (method != HeatingLoadCalculationMethod.En12831)
        {
            _logger.LogWarning("Unsupported heating load calculation method {CalculationMethod} for room {RoomId}.", method, room.RoomId);
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported heating load method.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var indoorTemperature = room.IndoorTemperatureC;
        var outdoorTemperature = buildingWinterDesignTemperatureC ??
            room.OutdoorTemperatureOverrideC ??
            _options.DefaultOutdoorHeatingDesignTemperatureC;
        var deltaT = Math.Max(indoorTemperature - outdoorTemperature, 0);

        var transmissionLoss = room.Walls
            .Where(wall => wall.IsExternal)
            .Sum(wall => wall.AreaM2 * GetWallUValue(wall) * deltaT);
        transmissionLoss += room.Windows.Sum(window => window.AreaM2 * window.UValue * deltaT);

        var airChangesPerHour = room.Ventilation?.AirChangesPerHour ?? _options.DefaultAirChangesPerHour;
        var heatRecoveryFactor = 1 - (room.Ventilation?.HeatRecoveryEfficiency ?? 0);
        var mechanicalVentilationLoss = _options.AirHeatCapacityWhPerM3K *
            airChangesPerHour *
            room.VolumeM3 *
            deltaT *
            heatRecoveryFactor;
        var infiltrationAirChangesPerHour = room.Ventilation is null
            ? 0
            : room.Ventilation.InfiltrationAirChangesPerHour +
            room.Ventilation.StackCoefficient * Math.Sqrt(deltaT);
        var infiltrationLoss = _options.AirHeatCapacityWhPerM3K *
            infiltrationAirChangesPerHour *
            room.VolumeM3 *
            deltaT;
        var ventilationLoss = mechanicalVentilationLoss + infiltrationLoss;
        var totalLoad = transmissionLoss + ventilationLoss;

        return Task.FromResult(new RoomHeatingLoadResult
        {
            RoomId = room.RoomId,
            RoomName = room.RoomName,
            CalculationMethod = method.ToString(),
            IndoorDesignTemperatureC = indoorTemperature,
            OutdoorDesignTemperatureC = outdoorTemperature,
            DeltaTemperatureC = Round(deltaT),
            VolumeM3 = Round(room.VolumeM3),
            AirChangesPerHour = Round(airChangesPerHour + infiltrationAirChangesPerHour),
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0)
        });
    }

    private static double GetWallUValue(WallHeatingReadModel wall)
    {
        if (wall.ConstructionLayers.Count == 0)
            return wall.UValue;

        var resistance = wall.ConstructionLayers.Sum(layer => layer.ThicknessM / layer.ThermalConductivityWPerMK);
        var totalResistance = InternalSurfaceResistance + resistance + ExternalSurfaceResistance;
        return totalResistance > 0 ? 1.0 / totalResistance : wall.UValue;
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
