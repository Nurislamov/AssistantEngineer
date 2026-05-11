using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

internal static class Iso52016PhysicalSurfaceExpandedRequestBuilder
{
    internal static Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        Iso52016PhysicalNodeModelOptions modelOptions,
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour)
    {
        var airNodeId = modelOptions.AirNodeId.Trim();
        var initialTemperatureC = heatBalanceOptions.InitialIndoorTemperatureC;
        var ventilationConductanceByHour = Iso52016PhysicalRoomModelMapping.BuildVentilationConductanceByHour(
            hourlyInputProfile,
            operationConditionsByHour);
        var hasVentilation = ventilationConductanceByHour.Values.Any(value => value > 0);
        var boundaryConditionsBySurface = Iso52016PhysicalRoomModelMapping.BuildBoundaryConditionLookup(surfaceBoundaryConditions);
        var surfacesWithHourlyBoundaryConditions = new HashSet<string>(
            boundaryConditionsBySurface.Keys,
            StringComparer.OrdinalIgnoreCase);

        var nodes = new List<Iso52016MatrixNodeDefinition>
        {
            new(
                NodeId: airNodeId,
                HeatCapacityJPerK: hourlyInputProfile.ThermalCapacityJPerK * modelOptions.AirHeatCapacityFraction,
                InitialTemperatureC: initialTemperatureC,
                IsAirNode: true)
        };

        var internalConductances = new List<Iso52016MatrixConductanceLink>();
        var boundaryConductances = new List<Iso52016MatrixBoundaryConductance>();

        foreach (var surface in surfaces)
        {
            var surfaceNodeId = Iso52016PhysicalRoomModelMapping.ResolveSurfaceNodeId(surface);
            var massNodeId = Iso52016PhysicalRoomModelMapping.ResolveMassNodeId(surface);
            var constructionCapacityJPerK = Iso52016PhysicalRoomModelMapping.CalculateConstructionHeatCapacityJPerK(surface);
            var surfaceHeatCapacityJPerK = surface.HeatCapacityJPerK ??
                Math.Max(
                    constructionCapacityJPerK * modelOptions.SurfaceNodeHeatCapacityFraction,
                    modelOptions.MinimumSurfaceNodeHeatCapacityJPerK);
            var massHeatCapacityJPerK = surface.MassHeatCapacityJPerK ??
                Math.Max(
                    constructionCapacityJPerK - surfaceHeatCapacityJPerK,
                    modelOptions.MinimumMassNodeHeatCapacityJPerK);
            var boundaryConductanceWPerK = surface.BoundaryConductanceWPerK ??
                Iso52016PhysicalRoomModelMapping.CalculateBoundaryConductanceWPerK(surface);
            var surfaceToAirConductanceWPerK = surface.SurfaceToAirConductanceWPerK ??
                surface.AreaM2 * modelOptions.DefaultSurfaceToAirConductanceWPerM2K;
            var surfaceToMassConductanceWPerK = surface.SurfaceToMassConductanceWPerK ??
                boundaryConductanceWPerK * modelOptions.SurfaceToMassConductanceMultiplier;

            nodes.Add(
                new Iso52016MatrixNodeDefinition(
                    NodeId: surfaceNodeId,
                    HeatCapacityJPerK: surfaceHeatCapacityJPerK,
                    InitialTemperatureC: initialTemperatureC));

            nodes.Add(
                new Iso52016MatrixNodeDefinition(
                    NodeId: massNodeId,
                    HeatCapacityJPerK: massHeatCapacityJPerK,
                    InitialTemperatureC: initialTemperatureC));

            internalConductances.Add(
                new Iso52016MatrixConductanceLink(
                    FromNodeId: airNodeId,
                    ToNodeId: surfaceNodeId,
                    ConductanceWPerK: surfaceToAirConductanceWPerK));

            internalConductances.Add(
                new Iso52016MatrixConductanceLink(
                    FromNodeId: surfaceNodeId,
                    ToNodeId: massNodeId,
                    ConductanceWPerK: surfaceToMassConductanceWPerK));

            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: surfaceNodeId,
                    BoundaryId: Iso52016PhysicalRoomModelMapping.ResolveBoundaryId(surface, modelOptions, surfacesWithHourlyBoundaryConditions),
                    ConductanceWPerK: boundaryConductanceWPerK));
        }

        if (hasVentilation)
        {
            boundaryConductances.Add(
                new Iso52016MatrixBoundaryConductance(
                    NodeId: airNodeId,
                    BoundaryId: modelOptions.VentilationBoundaryId.Trim(),
                    ConductanceWPerK: ventilationConductanceByHour.Values.Max()));
        }

        var solarDistribution = Iso52016PhysicalRoomModelMapping.BuildSurfaceDistribution(
            surfaces,
            static surface => surface.SolarGainsDistributionFraction);

        var internalRadiativeDistribution = Iso52016PhysicalRoomModelMapping.BuildSurfaceDistribution(
            surfaces,
            static surface => surface.InternalRadiativeGainsDistributionFraction);

        var hours = hourlyInputProfile.Hours
            .Select(hour =>
            {
                var boundaryTemperatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                var ventilationConductanceWPerK = ventilationConductanceByHour[hour.HourOfYear];
                var internalConvectiveFraction = Iso52016PhysicalRoomModelMapping.ResolveInternalGainsConvectiveFraction(
                    modelOptions,
                    hour,
                    operationConditionsByHour);
                var solarToAirFraction = Iso52016PhysicalRoomModelMapping.ResolveSolarGainsToAirFraction(
                    hour,
                    operationConditionsByHour);
                var internalConvectiveGainsW = hour.InternalGainsW * internalConvectiveFraction;
                var internalRadiativeGainsW = hour.InternalGainsW - internalConvectiveGainsW;
                var solarAirGainsW = hour.SolarGainsW * solarToAirFraction;
                var remainingSolarGainsW = hour.SolarGainsW - solarAirGainsW;
                var nodeHeatGains = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [airNodeId] = internalConvectiveGainsW + solarAirGainsW
                };

                if (hasVentilation)
                {
                    boundaryTemperatures[modelOptions.VentilationBoundaryId.Trim()] = Iso52016PhysicalRoomModelMapping.ResolveVentilationBoundaryTemperatureC(
                        hour,
                        operationConditionsByHour);
                }

                foreach (var surface in surfaces)
                {
                    var surfaceId = surface.SurfaceId.Trim();
                    var surfaceNodeId = Iso52016PhysicalRoomModelMapping.ResolveSurfaceNodeId(surface);
                    var boundaryId = Iso52016PhysicalRoomModelMapping.ResolveBoundaryId(surface, modelOptions, surfacesWithHourlyBoundaryConditions);

                    boundaryTemperatures[boundaryId] = Iso52016PhysicalRoomModelMapping.ResolveBoundaryTemperatureC(
                        surface,
                        modelOptions,
                        hour,
                        boundaryConditionsBySurface);

                    nodeHeatGains[surfaceNodeId] =
                        remainingSolarGainsW * solarDistribution[surfaceId] +
                        internalRadiativeGainsW * internalRadiativeDistribution[surfaceId];
                }

                return new Iso52016MatrixHourlyInputRecord(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    BoundaryTemperaturesC: boundaryTemperatures,
                    NodeHeatGainsW: nodeHeatGains,
                    HeatingSetpointC: hour.HeatingSetpointC,
                    CoolingSetpointC: hour.CoolingSetpointC,
                    BoundaryConductanceOverrides: hasVentilation
                        ? new[]
                        {
                            new Iso52016MatrixHourlyBoundaryConductanceOverride(
                                NodeId: airNodeId,
                                BoundaryId: modelOptions.VentilationBoundaryId.Trim(),
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
