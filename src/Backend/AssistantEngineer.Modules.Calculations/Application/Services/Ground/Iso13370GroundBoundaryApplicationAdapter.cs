using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class Iso13370GroundBoundaryApplicationAdapter
{
    public Iso13370GroundBoundaryInput BuildInput(
        Room room,
        double floorUValueWPerM2K,
        double indoorAnnualMeanTemperatureC,
        double outdoorAnnualMeanTemperatureC,
        IReadOnlyList<double>? outdoorMonthlyMeanTemperaturesC,
        double groundAnnualMeanTemperatureC,
        double groundTemperatureAmplitudeC,
        double groundTemperaturePhaseShiftMonths,
        double groundConductivityWPerMK = 2.0)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (room.GroundContactMetadata is null)
        {
            throw new InvalidOperationException(
                "Ground contact metadata is required to map to Iso13370GroundBoundaryInput.");
        }

        var metadata = room.GroundContactMetadata;
        return new Iso13370GroundBoundaryInput(
            AreaM2: Math.Max(room.Area.SquareMeters, 0.0),
            ExposedPerimeterM: metadata.ExposedPerimeterM,
            GroundConductivityWPerMK: groundConductivityWPerMK,
            FloorUValueWPerM2K: floorUValueWPerM2K,
            IndoorAnnualMeanTemperatureC: indoorAnnualMeanTemperatureC,
            OutdoorAnnualMeanTemperatureC: outdoorAnnualMeanTemperatureC,
            OutdoorMonthlyMeanTemperaturesC: outdoorMonthlyMeanTemperaturesC,
            GroundAnnualMeanTemperatureC: groundAnnualMeanTemperatureC,
            GroundTemperatureAmplitudeC: groundTemperatureAmplitudeC,
            GroundTemperaturePhaseShiftMonths: groundTemperaturePhaseShiftMonths,
            HorizontalInsulationWidthM: metadata.HorizontalInsulationWidthM,
            PerimeterInsulationDepthM: metadata.PerimeterInsulationDepthM,
            BurialDepthM: metadata.BurialDepthM,
            WallHeightBelowGradeM: metadata.WallHeightBelowGradeM,
            UnderfloorVentilationAirChangesPerHour: metadata.UnderfloorVentilationAirChangesPerHour,
            ContactKind: MapContactKind(metadata.ContactType));
    }

    private static Iso13370GroundContactKind MapContactKind(GroundContactType contactType) => contactType switch
    {
        GroundContactType.SlabOnGround => Iso13370GroundContactKind.SlabOnGround,
        GroundContactType.BasementConditioned => Iso13370GroundContactKind.ConditionedBasement,
        GroundContactType.BasementUnconditioned => Iso13370GroundContactKind.UnconditionedBasement,
        GroundContactType.CrawlSpace => Iso13370GroundContactKind.CrawlSpace,
        GroundContactType.VentilatedCrawlSpace => Iso13370GroundContactKind.VentilatedCrawlSpace,
        _ => Iso13370GroundContactKind.SlabOnGround
    };
}
