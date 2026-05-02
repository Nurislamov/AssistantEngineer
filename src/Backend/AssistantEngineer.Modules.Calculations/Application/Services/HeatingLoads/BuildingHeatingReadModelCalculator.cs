using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;

public sealed class BuildingHeatingReadModelCalculator
{
    private readonly En12831HeatingLoadOptions _options;
    private readonly TransmissionHeatTransferEngine _transmissionHeatTransfer;
    private readonly VentilationAndInfiltrationLoadEngine _ventilationLoads;
    private readonly ILogger<BuildingHeatingReadModelCalculator> _logger;

    public BuildingHeatingReadModelCalculator(
        IOptions<En12831HeatingLoadOptions> options,
        TransmissionHeatTransferEngine? transmissionHeatTransfer = null,
        VentilationAndInfiltrationLoadEngine? ventilationLoads = null,
        ILogger<BuildingHeatingReadModelCalculator>? logger = null)
    {
        _options = options.Value;
        _transmissionHeatTransfer = transmissionHeatTransfer ?? new TransmissionHeatTransferEngine();
        _ventilationLoads = ventilationLoads ?? new VentilationAndInfiltrationLoadEngine();
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

        var transmissionResult = _transmissionHeatTransfer.Calculate(
            RoomTransmissionInputFactory.CreateForReadModelRoom(
                room,
                outdoorTemperature));
        var transmissionLoss = transmissionResult.Value.TotalHeatLossW;

        var ventilationResult = _ventilationLoads.Calculate(
            CreateVentilationInput(room, indoorTemperature, outdoorTemperature, deltaT));
        var ventilationLoss = ventilationResult.Value.TotalHeatingLoadW;
        var airChangesPerHour =
            ventilationResult.Value.MechanicalVentilation.AirflowM3PerHour / Math.Max(room.VolumeM3, 0.001) +
            ventilationResult.Value.Infiltration.InfiltrationAirChangesPerHour;
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
            AirChangesPerHour = Round(airChangesPerHour),
            TransmissionHeatLossW = Round(transmissionLoss),
            VentilationHeatLossW = Round(ventilationLoss),
            MechanicalVentilationHeatLossW = Round(ventilationResult.Value.MechanicalVentilation.EffectiveHeatingLoadW),
            InfiltrationHeatLossW = Round(ventilationResult.Value.Infiltration.HeatingLoadW),
            NaturalVentilationHeatLossW = Round(ventilationResult.Value.NaturalVentilation.HeatingLoadW),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0)
        });
    }

    private VentilationAndInfiltrationLoadInput CreateVentilationInput(
        RoomHeatingReadModel room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double deltaT)
    {
        var infiltrationAirChangesPerHour = room.Ventilation is null
            ? 0
            : room.Ventilation.InfiltrationAirChangesPerHour +
            room.Ventilation.StackCoefficient * Math.Sqrt(deltaT);

        return new VentilationAndInfiltrationLoadInput(
            RoomId: room.RoomId,
            AreaM2: room.AreaM2,
            VolumeM3: room.VolumeM3,
            OccupancyPeople: 0,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            AirChangesPerHour: room.Ventilation?.AirChangesPerHour ?? _options.DefaultAirChangesPerHour,
            InfiltrationAirChangesPerHour: infiltrationAirChangesPerHour,
            HeatRecoveryEfficiency: room.Ventilation?.HeatRecoveryEfficiency ?? 0,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DiagnosticsContext: $"Read-model room {room.RoomId} heating ventilation");
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
