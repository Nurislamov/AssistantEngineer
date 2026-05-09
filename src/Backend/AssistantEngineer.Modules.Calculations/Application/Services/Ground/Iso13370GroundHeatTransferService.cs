using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class Iso13370GroundHeatTransferService : IGroundHeatTransferService
{
    private readonly Iso13370GroundHeatTransferOptions _options;
    private readonly Iso13370GroundBoundaryCalculator _calculator;
    private readonly Iso13370GroundBoundaryApplicationAdapter _adapter;
    private readonly Iso13370VirtualGroundTemperatureCalculator _virtualGroundCalculator;

    public Iso13370GroundHeatTransferService(
        IOptions<Iso13370GroundHeatTransferOptions> options,
        Iso13370GroundBoundaryCalculator calculator,
        Iso13370GroundBoundaryApplicationAdapter adapter,
        Iso13370VirtualGroundTemperatureCalculator virtualGroundCalculator)
    {
        _options = options.Value;
        _calculator = calculator;
        _adapter = adapter;
        _virtualGroundCalculator = virtualGroundCalculator;
    }

    public Iso13370GroundHeatTransferService(
        IOptions<Iso13370GroundHeatTransferOptions> options,
        Iso13370GroundBoundaryCalculator calculator,
        Iso13370GroundBoundaryApplicationAdapter adapter)
        : this(
            options,
            calculator,
            adapter,
            new Iso13370VirtualGroundTemperatureCalculator())
    {
    }

    public Iso13370GroundHeatTransferService(
        IOptions<Iso13370GroundHeatTransferOptions> options)
        : this(
            options,
            new Iso13370GroundBoundaryCalculator(new Iso13370GroundTemperatureProfileCalculator()),
            new Iso13370GroundBoundaryApplicationAdapter(),
            new Iso13370VirtualGroundTemperatureCalculator())
    {
    }

    public GroundBoundaryCondition CalculateBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        if (!_options.UseIso13370InspiredBoundaryCalculator)
            return CalculateCompatibilityBoundaryCondition(room, envelopeDefaults);

        if (room.GroundContactMetadata is null)
        {
            return BuildMatrixFallbackBoundaryCondition(room, envelopeDefaults);
        }

        if (_options.UseIso13370VirtualGroundTemperatureLane)
            return CalculateVirtualGroundBoundaryCondition(room, envelopeDefaults);

        return CalculateIso13370InspiredBoundaryCondition(room, envelopeDefaults);
    }

    private GroundBoundaryCondition CalculateCompatibilityBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        if (room.GroundContactMetadata is null)
            return BuildMatrixFallbackBoundaryCondition(room, envelopeDefaults);

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

    private GroundBoundaryCondition CalculateIso13370InspiredBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var input = _adapter.BuildInput(
            room,
            floorUValueWPerM2K: envelopeDefaults.FloorUValueWPerM2K,
            indoorAnnualMeanTemperatureC: _options.IndoorAnnualMeanTemperatureC,
            outdoorAnnualMeanTemperatureC: _options.OutdoorAnnualMeanTemperatureC,
            outdoorMonthlyMeanTemperaturesC: null,
            groundAnnualMeanTemperatureC: _options.GroundAnnualMeanTemperatureC,
            groundTemperatureAmplitudeC: _options.GroundTemperatureAmplitudeC,
            groundTemperaturePhaseShiftMonths: _options.GroundTemperaturePhaseShiftMonths,
            groundConductivityWPerMK: _options.GroundConductivityWPerMK);

        var result = _calculator.Calculate(input);

        return new GroundBoundaryCondition
        {
            HeatTransferCoefficientWPerK = result.HeatTransferCoefficientWPerK,
            GroundTemperatureWeight = result.GroundWeight,
            OutdoorTemperatureWeight = result.OutdoorWeight,
            IndoorTemperatureWeight = result.IndoorWeight
        };
    }

    private GroundBoundaryCondition CalculateVirtualGroundBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var virtualInput = _adapter.BuildVirtualGroundInput(
            room,
            floorUValueWPerM2K: envelopeDefaults.FloorUValueWPerM2K,
            annualAverageOutdoorTemperatureC: _options.OutdoorAnnualMeanTemperatureC,
            monthlyOutdoorTemperatureProfileC: null,
            seasonalAmplitudeC: _options.VirtualGroundSeasonalAmplitudeC,
            seasonalPhaseShiftMonths: _options.VirtualGroundSeasonalPhaseShiftMonths,
            groundConductivityWPerMK: _options.GroundConductivityWPerMK,
            equivalentGroundThicknessM: _options.VirtualGroundEquivalentGroundThicknessM > 0.0
                ? _options.VirtualGroundEquivalentGroundThicknessM
                : null,
            enablePerimeterThermalBridge: _options.UseIso13370VirtualGroundPerimeterThermalBridge,
            thermalBridgeLinearTransmittanceWPerMK: _options.VirtualGroundThermalBridgeLinearTransmittanceWPerMK,
            options: new Iso13370GroundCalculationOptions(
                EnableSeasonalComponent: _options.VirtualGroundEnableSeasonalComponent,
                EnablePerimeterThermalBridge: _options.VirtualGroundEnablePerimeterThermalBridge,
                SeasonalAttenuationFactor: _options.VirtualGroundSeasonalAttenuationFactor,
                MonthlyHeatTransferVariationFactor: _options.VirtualGroundMonthlyHeatTransferVariationFactor,
                MinimumEquivalentGroundThicknessM: _options.VirtualGroundMinimumEquivalentGroundThicknessM,
                MaximumEquivalentGroundThicknessM: _options.VirtualGroundMaximumEquivalentGroundThicknessM));

        var result = _virtualGroundCalculator.Calculate(virtualInput);
        var metadata = room.GroundContactMetadata!;
        var (groundWeight, outdoorWeight, indoorWeight) = GetBoundaryWeights(
            metadata.ContactType,
            metadata.UnderfloorVentilationAirChangesPerHour);

        return new GroundBoundaryCondition
        {
            HeatTransferCoefficientWPerK = result.AnnualEquivalentGroundHeatTransferCoefficientWPerK,
            GroundTemperatureWeight = groundWeight,
            OutdoorTemperatureWeight = outdoorWeight,
            IndoorTemperatureWeight = indoorWeight
        };
    }

    private static GroundBoundaryCondition BuildMatrixFallbackBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        return new GroundBoundaryCondition
        {
            HeatTransferCoefficientWPerK = GetMatrixGroundHeatTransferCoefficient(room, envelopeDefaults),
            GroundTemperatureWeight = 1.0,
            OutdoorTemperatureWeight = 0.0,
            IndoorTemperatureWeight = 0.0
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

    private static double GetMatrixGroundHeatTransferCoefficient(Room room, BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * RoomTransmissionInputFactory.ResolveWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;
        return envelope;
    }
}
