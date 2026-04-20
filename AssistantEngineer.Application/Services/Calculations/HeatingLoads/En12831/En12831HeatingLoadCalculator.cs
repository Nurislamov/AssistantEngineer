using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Calculations;

public interface IRoomHeatingLoadCalculator
{
    Task<RoomHeatingLoadResult> CalculateAsync(
        Room room,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

public interface IBuildingHeatingLoadCalculator
{
    Task<BuildingHeatingLoadResult> CalculateAsync(
        Building building,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

public sealed class En12831HeatingLoadOptions
{
    public double DefaultAirChangesPerHour { get; init; } = 0.5;
    public double AirHeatCapacityWhPerM3K { get; init; } = 0.34;
}

public sealed class En12831HeatingLoadCalculator : IRoomHeatingLoadCalculator, IBuildingHeatingLoadCalculator
{
    private readonly En12831HeatingLoadOptions _options;
    private readonly ILogger<En12831HeatingLoadCalculator> _logger;

    public En12831HeatingLoadCalculator(
        En12831HeatingLoadOptions options,
        ILogger<En12831HeatingLoadCalculator>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<En12831HeatingLoadCalculator>.Instance;
    }

    public Task<RoomHeatingLoadResult> CalculateAsync(
        Room room,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        if (method != HeatingLoadCalculationMethod.En12831)
        {
            _logger.LogWarning("Unsupported heating load calculation method {CalculationMethod} for room {RoomId}.", method, room.Id);
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported heating load method.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("EN 12831 heating calculation started for room {RoomId}.", room.Id);

        var indoorTemperature = room.IndoorTemperature.Celsius;
        var outdoorTemperature = GetOutdoorDesignTemperature(room);
        var deltaT = Math.Max(indoorTemperature - outdoorTemperature, 0);
        var transmissionLoss = 0.0;

        foreach (var wall in room.Walls.Where(wall => wall.IsExternal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            transmissionLoss += wall.Area.SquareMeters * GetWallUValue(wall) * deltaT;
        }

        foreach (var window in room.Windows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            transmissionLoss += window.Area.SquareMeters * window.UValue.Value * deltaT;
        }

        var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ??
            _options.DefaultAirChangesPerHour;
        var heatRecoveryFactor = 1 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0);
        var mechanicalVentilationLoss = _options.AirHeatCapacityWhPerM3K *
            airChangesPerHour *
            room.CalculateVolume() *
            deltaT *
            heatRecoveryFactor;
        var infiltrationAirChangesPerHour = room.VentilationParameters is null
            ? 0
            : room.VentilationParameters.InfiltrationAirChangesPerHour +
            room.VentilationParameters.StackCoefficient * Math.Sqrt(deltaT);
        var infiltrationLoss = _options.AirHeatCapacityWhPerM3K *
            infiltrationAirChangesPerHour *
            room.CalculateVolume() *
            deltaT;
        var ventilationLoss = mechanicalVentilationLoss + infiltrationLoss;
        var totalLoad = transmissionLoss + ventilationLoss;

        var result = new RoomHeatingLoadResult
        {
            RoomId = room.Id,
            RoomName = room.Name,
            CalculationMethod = method.ToString(),
            IndoorDesignTemperatureC = indoorTemperature,
            OutdoorDesignTemperatureC = outdoorTemperature,
            DeltaTemperatureC = Round(deltaT),
            VolumeM3 = Round(room.CalculateVolume()),
            AirChangesPerHour = Round(airChangesPerHour + infiltrationAirChangesPerHour),
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0)
        };
        _logger.LogDebug(
            "EN 12831 heating calculation finished for room {RoomId}: total design load {TotalDesignHeatingLoadW} W.",
            room.Id,
            result.TotalDesignHeatingLoadW);
        return Task.FromResult(result);
    }

    public async Task<BuildingHeatingLoadResult> CalculateAsync(
        Building building,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        if (method != HeatingLoadCalculationMethod.En12831)
        {
            _logger.LogWarning(
                "Unsupported heating load calculation method {CalculationMethod} for building {BuildingId}.",
                method,
                building.Id);
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported heating load method.");
        }

        _logger.LogDebug("EN 12831 heating calculation started for building {BuildingId}.", building.Id);

        var rooms = new List<RoomHeatingLoadResult>();
        foreach (var floor in building.Floors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var room in floor.Rooms)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rooms.Add(await CalculateAsync(room, method, preferences, cancellationToken));
            }
        }

        var transmissionLoss = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilationLoss = rooms.Sum(room => room.VentilationHeatLossW);
        var totalLoad = transmissionLoss + ventilationLoss;

        _logger.LogDebug(
            "EN 12831 heating calculation finished for building {BuildingId}: rooms {RoomCount}, total design load {TotalDesignHeatingLoadW} W.",
            building.Id,
            rooms.Count,
            Round(totalLoad));

        return new BuildingHeatingLoadResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = method.ToString(),
            RoomsCount = rooms.Count,
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0),
            Rooms = rooms
        };
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static double GetOutdoorDesignTemperature(Room room) =>
        room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
        room.OutdoorTemperature.Celsius;

    private static double GetWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;
}
