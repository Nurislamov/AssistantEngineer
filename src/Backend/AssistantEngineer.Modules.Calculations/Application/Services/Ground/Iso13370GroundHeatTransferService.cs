using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class Iso13370GroundHeatTransferService : IGroundHeatTransferService
{
    private readonly Iso13370GroundHeatTransferOptions _options;

    public Iso13370GroundHeatTransferService(IOptions<Iso13370GroundHeatTransferOptions> options)
    {
        _options = options.Value;
    }

    public GroundBoundaryCondition CalculateBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        if (room.GroundContactMetadata is null)
        {
            return new GroundBoundaryCondition
            {
                HeatTransferCoefficientWPerK = GetLegacyGroundHeatTransferCoefficient(room, envelopeDefaults),
                GroundTemperatureWeight = 1.0,
                OutdoorTemperatureWeight = 0.0,
                IndoorTemperatureWeight = 0.0
            };
        }
        var metadata = room.GroundContactMetadata;
        var area = Math.Max(room.Area.SquareMeters, 0.1);
        var perimeter = Math.Max(metadata.ExposedPerimeterM, 0.1);
        var characteristicDimension = Math.Max(area / (0.5 * perimeter), 0.1);

        var insulationEffect = 1.0 - Math.Min(
            0.55,
            metadata.HorizontalInsulationWidthM * 0.12 + metadata.PerimeterInsulationDepthM * 0.08);

        var burialEffect = 1.0 + Math.Min(0.45, metadata.BurialDepthM * 0.12 + metadata.WallHeightBelowGradeM * 0.08);
        var perimeterEffect = 1.0 + _options.PerimeterAmplificationFactor / (1.0 + characteristicDimension);

        var equivalentU = (_options.GroundConductivityWPerMK /
                           (characteristicDimension + _options.BaseCharacteristicDepthM))
                          * perimeterEffect
                          * burialEffect
                          * insulationEffect
                          * GetContactFactor(metadata.ContactType)
                          * GetVentilationModifier(metadata.ContactType, metadata.UnderfloorVentilationAirChangesPerHour);
        var heatTransfer = Math.Max(0.01, equivalentU * area);
        var (groundWeight, outdoorWeight, indoorWeight) = GetBoundaryWeights(
            metadata.ContactType,
            metadata.UnderfloorVentilationAirChangesPerHour);

        return new GroundBoundaryCondition
        {
            HeatTransferCoefficientWPerK = heatTransfer,
            GroundTemperatureWeight = groundWeight,
            OutdoorTemperatureWeight = outdoorWeight,
            IndoorTemperatureWeight = indoorWeight
        };
    }

    private double GetContactFactor(GroundContactType type) => type switch
    {
        GroundContactType.SlabOnGround => _options.SlabOnGroundFactor,
        GroundContactType.BasementConditioned => _options.BasementConditionedFactor,
        GroundContactType.BasementUnconditioned => _options.BasementUnconditionedFactor,
        GroundContactType.CrawlSpace => _options.CrawlSpaceFactor,
        GroundContactType.VentilatedCrawlSpace => _options.VentilatedCrawlSpaceFactor,
        _ => 1.0
    };

    private static double GetVentilationModifier(GroundContactType type, double underfloorVentilationAch) => type switch
    {
        GroundContactType.CrawlSpace => 1.0 + Math.Min(0.15, underfloorVentilationAch * 0.03),
        GroundContactType.VentilatedCrawlSpace => 1.0 + Math.Min(0.35, underfloorVentilationAch * 0.05),
        _ => 1.0
    };

    private static (double ground, double outdoor, double indoor) GetBoundaryWeights(
        GroundContactType type,
        double underfloorVentilationAch)
    {
        return type switch
        {
            GroundContactType.SlabOnGround => (1.0, 0.0, 0.0),
            GroundContactType.BasementConditioned => (0.35, 0.0, 0.65),
            GroundContactType.BasementUnconditioned => (0.50, 0.50, 0.0),
            GroundContactType.CrawlSpace =>
                (Math.Max(0.45, 0.75 - underfloorVentilationAch * 0.03),
                    Math.Min(0.55, 0.25 + underfloorVentilationAch * 0.03),
                    0.0),
            GroundContactType.VentilatedCrawlSpace =>
                (Math.Max(0.15, 0.35 - underfloorVentilationAch * 0.04),
                    Math.Min(0.85, 0.65 + underfloorVentilationAch * 0.04),
                    0.0),
            _ => (1.0, 0.0, 0.0)
        };
    }

    private static double GetLegacyGroundHeatTransferCoefficient(Room room, BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * Services.Iso52016.Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;
        return envelope;
    }
}