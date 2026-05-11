using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

internal static class Iso52016PhysicalRoomModelMapping
{
    internal static IReadOnlyDictionary<string, IReadOnlyDictionary<int, double>> BuildBoundaryConditionLookup(
        IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition> surfaceBoundaryConditions)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<int, double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in surfaceBoundaryConditions.GroupBy(condition => condition.SurfaceId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            result[group.Key] = group.ToDictionary(
                condition => condition.HourOfYear,
                condition => condition.BoundaryTemperatureC);
        }

        return result;
    }

    internal static IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> BuildOperationConditionLookup(
        IReadOnlyList<Iso52016PhysicalHourlyOperationCondition> operationConditions) =>
        operationConditions.ToDictionary(
            condition => condition.HourOfYear,
            condition => condition);

    internal static IReadOnlyDictionary<int, double> BuildVentilationConductanceByHour(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        hourlyInputProfile.Hours.ToDictionary(
            hour => hour.HourOfYear,
            hour => operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
                operationCondition.VentilationHeatTransferCoefficientWPerK.HasValue
                    ? operationCondition.VentilationHeatTransferCoefficientWPerK.Value
                    : hour.VentilationHeatTransferCoefficientWPerK);

    internal static double ResolveInternalGainsConvectiveFraction(
        Iso52016PhysicalNodeModelOptions modelOptions,
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
            operationCondition.InternalGainsConvectiveFraction.HasValue
                ? operationCondition.InternalGainsConvectiveFraction.Value
                : modelOptions.InternalGainsConvectiveFraction;

    internal static double ResolveSolarGainsToAirFraction(
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
            operationCondition.SolarGainsToAirFraction.HasValue
                ? operationCondition.SolarGainsToAirFraction.Value
                : 0.0;

    internal static double ResolveVentilationBoundaryTemperatureC(
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<int, Iso52016PhysicalHourlyOperationCondition> operationConditionsByHour) =>
        operationConditionsByHour.TryGetValue(hour.HourOfYear, out var operationCondition) &&
            operationCondition.VentilationBoundaryTemperatureC.HasValue
                ? operationCondition.VentilationBoundaryTemperatureC.Value
                : hour.OutdoorTemperatureC;

    internal static IReadOnlyDictionary<string, double> BuildSurfaceDistribution(
        IReadOnlyList<Iso52016PhysicalSurface> surfaces,
        Func<Iso52016PhysicalSurface, double?> selector)
    {
        if (surfaces.All(surface => selector(surface).HasValue))
        {
            return surfaces.ToDictionary(
                surface => surface.SurfaceId.Trim(),
                surface => selector(surface)!.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        var totalAreaM2 = surfaces.Sum(surface => surface.AreaM2);

        return surfaces.ToDictionary(
            surface => surface.SurfaceId.Trim(),
            surface => surface.AreaM2 / totalAreaM2,
            StringComparer.OrdinalIgnoreCase);
    }

    internal static string ResolveSurfaceNodeId(
        Iso52016PhysicalSurface surface) =>
        string.IsNullOrWhiteSpace(surface.SurfaceNodeId)
            ? $"surface:{surface.SurfaceId.Trim()}"
            : surface.SurfaceNodeId.Trim();

    internal static string ResolveMassNodeId(
        Iso52016PhysicalSurface surface) =>
        string.IsNullOrWhiteSpace(surface.MassNodeId)
            ? $"mass:{surface.SurfaceId.Trim()}"
            : surface.MassNodeId.Trim();

    internal static string ResolveBoundaryId(
        Iso52016PhysicalSurface surface,
        Iso52016PhysicalNodeModelOptions modelOptions,
        IReadOnlySet<string> surfacesWithHourlyBoundaryConditions)
    {
        if (!string.IsNullOrWhiteSpace(surface.BoundaryId))
            return surface.BoundaryId.Trim();

        var baseBoundaryId = ResolveDefaultBoundaryId(surface, modelOptions);
        var surfaceId = surface.SurfaceId.Trim();

        return surfacesWithHourlyBoundaryConditions.Contains(surfaceId)
            ? $"{baseBoundaryId}:{surfaceId}"
            : baseBoundaryId;
    }

    private static string ResolveDefaultBoundaryId(
        Iso52016PhysicalSurface surface,
        Iso52016PhysicalNodeModelOptions modelOptions) =>
        surface.BoundaryType switch
        {
            Iso52016PhysicalSurfaceBoundaryType.Outdoor => modelOptions.OutdoorBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.Ground => modelOptions.GroundBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned => modelOptions.AdjacentConditionedBoundaryId.Trim(),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned => modelOptions.AdjacentUnconditionedBoundaryId.Trim(),
            _ => throw new ArgumentOutOfRangeException(nameof(surface), "Unsupported physical surface boundary type.")
        };

    internal static double ResolveBoundaryTemperatureC(
        Iso52016PhysicalSurface surface,
        Iso52016PhysicalNodeModelOptions modelOptions,
        Iso52016RoomHourlyInputRecord hour,
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, double>> boundaryConditionsBySurface)
    {
        var surfaceId = surface.SurfaceId.Trim();

        if (boundaryConditionsBySurface.TryGetValue(surfaceId, out var byHour) &&
            byHour.TryGetValue(hour.HourOfYear, out var boundaryTemperatureC))
        {
            return boundaryTemperatureC;
        }

        return surface.BoundaryType switch
        {
            Iso52016PhysicalSurfaceBoundaryType.Outdoor => hour.OutdoorTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.Ground => hour.GroundBoundaryTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned => surface.AdjacentBoundaryTemperatureC ?? modelOptions.AdjacentBoundaryTemperatureC,
            Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned => surface.AdjacentBoundaryTemperatureC ?? modelOptions.AdjacentBoundaryTemperatureC,
            _ => throw new ArgumentOutOfRangeException(nameof(surface), "Unsupported physical surface boundary type.")
        };
    }

    internal static double CalculateBoundaryConductanceWPerK(
        Iso52016PhysicalSurface surface)
    {
        var resistanceM2KPerW = surface.ConstructionLayers.Sum(layer => layer.ThicknessM / layer.ConductivityWPerMK);

        if (resistanceM2KPerW <= 0)
            throw new InvalidOperationException($"ISO 52016 physical room model surface '{surface.SurfaceId}' construction resistance must be greater than zero.");

        return surface.AreaM2 / resistanceM2KPerW;
    }

    internal static double CalculateConstructionHeatCapacityJPerK(
        Iso52016PhysicalSurface surface) =>
        surface.ConstructionLayers.Sum(layer =>
            layer.ThicknessM *
            surface.AreaM2 *
            layer.DensityKgPerM3 *
            layer.SpecificHeatCapacityJPerKgK);
}
