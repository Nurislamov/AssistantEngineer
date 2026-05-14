using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

internal static class ThermalZoneBoundaryAggregation
{
    public static ThermalZoneBoundaryTotals AggregateSurfaceTotals(
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> surfaces)
    {
        var totals = new ThermalZoneBoundaryTotals();

        foreach (var surface in surfaces)
        {
            var coefficient = surface.HeatTransferCoefficientWPerKelvin ?? 0.0;
            if (!double.IsFinite(coefficient) || coefficient <= 0.0)
            {
                continue;
            }

            totals.TotalHeatTransferCoefficientWPerKelvin += coefficient;
            switch (surface.BoundaryKind)
            {
                case ThermalBoundaryKind.Outdoor:
                    totals.OutdoorHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.Ground:
                    totals.GroundHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.AdjacentConditionedZone:
                    totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.AdjacentUnconditionedZone:
                    totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
                case ThermalBoundaryKind.InternalPartition:
                    totals.InternalPartitionHeatTransferCoefficientWPerKelvin += coefficient;
                    break;
            }
        }

        totals.AdiabaticAreaSquareMeters += surfaces
            .Where(surface => surface.IsAdiabatic)
            .Sum(surface => Math.Max(0.0, surface.AreaSquareMeters));

        return totals;
    }

    public static ThermalZoneBoundaryTotals AggregateZoneTotals(
        IReadOnlyList<ThermalRoomBoundaryCalculationResult> rooms,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> unassignedSurfaces)
    {
        var totals = new ThermalZoneBoundaryTotals();

        foreach (var room in rooms)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += room.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += room.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += room.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += room.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += room.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += room.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += room.AdiabaticAreaSquareMeters;
        }

        var surfaceTotals = AggregateSurfaceTotals(unassignedSurfaces);
        totals.TotalHeatTransferCoefficientWPerKelvin += surfaceTotals.TotalHeatTransferCoefficientWPerKelvin;
        totals.OutdoorHeatTransferCoefficientWPerKelvin += surfaceTotals.OutdoorHeatTransferCoefficientWPerKelvin;
        totals.GroundHeatTransferCoefficientWPerKelvin += surfaceTotals.GroundHeatTransferCoefficientWPerKelvin;
        totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += surfaceTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
        totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += surfaceTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
        totals.InternalPartitionHeatTransferCoefficientWPerKelvin += surfaceTotals.InternalPartitionHeatTransferCoefficientWPerKelvin;
        totals.AdiabaticAreaSquareMeters += surfaceTotals.AdiabaticAreaSquareMeters;

        return totals;
    }

    public static ThermalZoneBoundaryTotals AggregateBuildingTotals(
        IReadOnlyList<ThermalZoneBoundaryCalculationResult> zones,
        IReadOnlyList<ThermalRoomBoundaryCalculationResult> unassignedRooms,
        IReadOnlyList<ThermalSurfaceBoundaryCalculationResult> unassignedSurfaces)
    {
        var totals = new ThermalZoneBoundaryTotals();

        foreach (var zone in zones)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += zone.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += zone.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += zone.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += zone.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += zone.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += zone.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += zone.AdiabaticAreaSquareMeters;
        }

        foreach (var room in unassignedRooms)
        {
            totals.TotalHeatTransferCoefficientWPerKelvin += room.TotalHeatTransferCoefficientWPerKelvin;
            totals.OutdoorHeatTransferCoefficientWPerKelvin += room.OutdoorHeatTransferCoefficientWPerKelvin;
            totals.GroundHeatTransferCoefficientWPerKelvin += room.GroundHeatTransferCoefficientWPerKelvin;
            totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += room.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
            totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += room.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
            totals.InternalPartitionHeatTransferCoefficientWPerKelvin += room.InternalPartitionHeatTransferCoefficientWPerKelvin;
            totals.AdiabaticAreaSquareMeters += room.AdiabaticAreaSquareMeters;
        }

        var unassignedSurfaceTotals = AggregateSurfaceTotals(unassignedSurfaces);
        totals.TotalHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.TotalHeatTransferCoefficientWPerKelvin;
        totals.OutdoorHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.OutdoorHeatTransferCoefficientWPerKelvin;
        totals.GroundHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.GroundHeatTransferCoefficientWPerKelvin;
        totals.AdjacentConditionedHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.AdjacentConditionedHeatTransferCoefficientWPerKelvin;
        totals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.AdjacentUnconditionedHeatTransferCoefficientWPerKelvin;
        totals.InternalPartitionHeatTransferCoefficientWPerKelvin += unassignedSurfaceTotals.InternalPartitionHeatTransferCoefficientWPerKelvin;
        totals.AdiabaticAreaSquareMeters += unassignedSurfaceTotals.AdiabaticAreaSquareMeters;

        return totals;
    }
}

internal sealed class ThermalZoneBoundaryTotals
{
    public double TotalHeatTransferCoefficientWPerKelvin { get; set; }

    public double OutdoorHeatTransferCoefficientWPerKelvin { get; set; }

    public double GroundHeatTransferCoefficientWPerKelvin { get; set; }

    public double AdjacentConditionedHeatTransferCoefficientWPerKelvin { get; set; }

    public double AdjacentUnconditionedHeatTransferCoefficientWPerKelvin { get; set; }

    public double InternalPartitionHeatTransferCoefficientWPerKelvin { get; set; }

    public double AdiabaticAreaSquareMeters { get; set; }
}
