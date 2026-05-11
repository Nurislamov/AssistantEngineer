using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

internal static class Iso52016PhysicalThreeNodeRequestBuilder
{
    internal static Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        Iso52016PhysicalNodeModelOptions modelOptions,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour)
    {
        var airNodeId = modelOptions.AirNodeId.Trim();
        var internalSurfaceNodeId = modelOptions.InternalSurfaceNodeId.Trim();
        var thermalMassNodeId = modelOptions.ThermalMassNodeId.Trim();
        var outdoorBoundaryId = modelOptions.OutdoorBoundaryId.Trim();
        var groundBoundaryId = modelOptions.GroundBoundaryId.Trim();
        var adjacentBoundaryId = modelOptions.AdjacentBoundaryId.Trim();
        var ventilationBoundaryId = modelOptions.VentilationBoundaryId.Trim();
        var initialTemperatureC = heatBalanceOptions.InitialIndoorTemperatureC;
        var totalCapacityJPerK = hourlyInputProfile.ThermalCapacityJPerK;
        var ventilationConductanceByHour = Iso52016PhysicalRoomModelMapping.BuildVentilationConductanceByHour(
            hourlyInputProfile,
            operationConditionsByHour);
        var hasVentilation = ventilationConductanceByHour.Values.Any(value => value > 0);

        var nodes = new[]
        {
            new Iso52016MatrixNodeDefinition(
                NodeId: airNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.AirHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC,
                IsAirNode: true),
            new Iso52016MatrixNodeDefinition(
                NodeId: internalSurfaceNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.InternalSurfaceHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC),
            new Iso52016MatrixNodeDefinition(
                NodeId: thermalMassNodeId,
                HeatCapacityJPerK: totalCapacityJPerK * modelOptions.ThermalMassHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC)
        };

        var airToSurfaceConductanceWPerK =
            modelOptions.AirToInternalSurfaceConductanceWPerK ??
            hourlyInputProfile.TotalHeatTransferCoefficientWPerK * modelOptions.AirToInternalSurfaceConductanceMultiplier;

        var surfaceToMassConductanceWPerK =
            modelOptions.InternalSurfaceToThermalMassConductanceWPerK ??
            hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK * modelOptions.InternalSurfaceToThermalMassConductanceMultiplier;

        var internalConductances = new[]
        {
            new Iso52016MatrixConductanceLink(
                FromNodeId: airNodeId,
                ToNodeId: internalSurfaceNodeId,
                ConductanceWPerK: airToSurfaceConductanceWPerK),
            new Iso52016MatrixConductanceLink(
                FromNodeId: internalSurfaceNodeId,
                ToNodeId: thermalMassNodeId,
                ConductanceWPerK: surfaceToMassConductanceWPerK)
        };

        var boundaryConductances = new List<Iso52016MatrixBoundaryConductance>
        {
            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: outdoorBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK * modelOptions.OutdoorTransmissionConductanceFraction),
            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: groundBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK * modelOptions.GroundTransmissionConductanceFraction),
            new(
                NodeId: internalSurfaceNodeId,
                BoundaryId: adjacentBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TransmissionHeatTransferCoefficientWPerK * modelOptions.AdjacentTransmissionConductanceFraction)
        };

        if (hasVentilation)
        {
            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: airNodeId,
                    BoundaryId: ventilationBoundaryId,
                    ConductanceWPerK: ventilationConductanceByHour.Values.Max()));
        }

        var hours = hourlyInputProfile.Hours
            .Select(hour =>
            {
                var internalConvectiveFraction = Iso52016PhysicalRoomModelMapping.ResolveInternalGainsConvectiveFraction(
                    modelOptions,
                    hour,
                    operationConditionsByHour);
                var solarToAirFraction = Iso52016PhysicalRoomModelMapping.ResolveSolarGainsToAirFraction(
                    hour,
                    operationConditionsByHour);
                var ventilationConductanceWPerK = ventilationConductanceByHour[hour.HourOfYear];

                var internalConvectiveGainsW = hour.InternalGainsW * internalConvectiveFraction;
                var internalRadiativeGainsW = hour.InternalGainsW - internalConvectiveGainsW;
                var solarAirGainsW = hour.SolarGainsW * solarToAirFraction;
                var remainingSolarGainsW = hour.SolarGainsW - solarAirGainsW;

                var boundaryTemperatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [outdoorBoundaryId] = hour.OutdoorTemperatureC,
                    [groundBoundaryId] = hour.GroundBoundaryTemperatureC,
                    [adjacentBoundaryId] = modelOptions.AdjacentBoundaryTemperatureC
                };

                if (hasVentilation)
                {
                    boundaryTemperatures[ventilationBoundaryId] = Iso52016PhysicalRoomModelMapping.ResolveVentilationBoundaryTemperatureC(
                        hour,
                        operationConditionsByHour);
                }

                return new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    BoundaryTemperaturesC: boundaryTemperatures,
                    NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        [airNodeId] = internalConvectiveGainsW + solarAirGainsW,
                        [internalSurfaceNodeId] = remainingSolarGainsW * modelOptions.SolarGainsToInternalSurfaceFraction +
                            internalRadiativeGainsW * modelOptions.InternalRadiativeGainsToInternalSurfaceFraction,
                        [thermalMassNodeId] = remainingSolarGainsW * (1.0 - modelOptions.SolarGainsToInternalSurfaceFraction) +
                            internalRadiativeGainsW * (1.0 - modelOptions.InternalRadiativeGainsToInternalSurfaceFraction)
                    },
                    HeatingSetpointC: hour.HeatingSetpointC,
                    CoolingSetpointC: hour.CoolingSetpointC,
                    BoundaryConductanceOverrides: hasVentilation
                        ? new[]
                        {
                            new Iso52016MatrixHourlyBoundaryConductanceOverride(
                                NodeId: airNodeId,
                                BoundaryId: ventilationBoundaryId,
                                ConductanceWPerK: ventilationConductanceWPerK)
                        }
                        : null);
            })
            .ToArray();

        return Iso52016PhysicalRoomModelRequestFactory.CreateSuccessRequest(
            hourlyInputProfile,
            heatBalanceOptions,
            airNodeId,
            nodes,
            internalConductances,
            boundaryConductances,
            hours);
    }
}
