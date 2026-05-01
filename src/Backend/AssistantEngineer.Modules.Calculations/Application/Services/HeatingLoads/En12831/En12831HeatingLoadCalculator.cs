using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;

public sealed class En12831HeatingLoadCalculator : 
    IRoomHeatingLoadCalculator, IBuildingHeatingLoadCalculator
{
    private readonly En12831HeatingLoadOptions _options;
    private readonly TransmissionHeatTransferEngine _transmissionHeatTransfer;
    private readonly VentilationAndInfiltrationLoadEngine _ventilationLoads;
    private readonly ILogger<En12831HeatingLoadCalculator> _logger;

    public En12831HeatingLoadCalculator(
        IOptions<En12831HeatingLoadOptions> options,
        TransmissionHeatTransferEngine? transmissionHeatTransfer = null,
        VentilationAndInfiltrationLoadEngine? ventilationLoads = null,
        ILogger<En12831HeatingLoadCalculator>? logger = null)
    {
        _options = options.Value;
        _transmissionHeatTransfer = transmissionHeatTransfer ?? new TransmissionHeatTransferEngine();
        _ventilationLoads = ventilationLoads ?? new VentilationAndInfiltrationLoadEngine();
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
        var transmissionResult = _transmissionHeatTransfer.Calculate(
            RoomTransmissionInputFactory.CreateForRoom(
                room,
                indoorTemperature,
                outdoorTemperature));
        var transmissionLoss = transmissionResult.Value.TotalHeatLossW;
        LogCalculationDiagnostics(room.Id, transmissionResult.Value.Diagnostics);

        var ventilationResult = _ventilationLoads.Calculate(
            CreateVentilationInput(room, indoorTemperature, outdoorTemperature, deltaT));
        var ventilationLoss = ventilationResult.Value.TotalHeatingLoadW;
        var airChangesPerHour =
            ventilationResult.Value.MechanicalVentilation.AirflowM3PerHour / Math.Max(room.CalculateVolume(), 0.001) +
            ventilationResult.Value.Infiltration.InfiltrationAirChangesPerHour;
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
            AirChangesPerHour = Round(airChangesPerHour),
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            MechanicalVentilationHeatLossW = Round(ventilationResult.Value.MechanicalVentilation.EffectiveHeatingLoadW),
            InfiltrationHeatLossW = Round(ventilationResult.Value.Infiltration.HeatingLoadW),
            NaturalVentilationHeatLossW = Round(ventilationResult.Value.NaturalVentilation.HeatingLoadW),
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

    private VentilationAndInfiltrationLoadInput CreateVentilationInput(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double deltaT)
    {
        var infiltrationAirChangesPerHour = room.VentilationParameters is null
            ? 0
            : room.VentilationParameters.InfiltrationAirChangesPerHour +
              room.VentilationParameters.StackCoefficient * Math.Sqrt(deltaT);

        return new VentilationAndInfiltrationLoadInput(
            RoomId: room.Id,
            AreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OccupancyPeople: room.PeopleCount,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            AirChangesPerHour: room.VentilationParameters?.AirChangesPerHour ?? _options.DefaultAirChangesPerHour,
            InfiltrationAirChangesPerHour: infiltrationAirChangesPerHour,
            HeatRecoveryEfficiency: room.VentilationParameters?.HeatRecoveryEfficiency ?? 0,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DiagnosticsContext: $"Room {room.Id} heating ventilation");
    }

    private double GetOutdoorDesignTemperature(Room room) =>
        room.Floor.Building.ClimateZone?.WinterDesignTemperature.Celsius ??
        room.OutdoorTemperatureOverride?.Celsius ??
        _options.DefaultOutdoorHeatingDesignTemperatureC;

    private void LogCalculationDiagnostics(
        int roomId,
        IReadOnlyList<CalculationDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics.Where(diagnostic =>
                     diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            _logger.LogWarning(
                "Transmission heat transfer diagnostic for room {RoomId}: {Code} {Message}",
                roomId,
                diagnostic.Code,
                diagnostic.Message);
        }
    }
}
