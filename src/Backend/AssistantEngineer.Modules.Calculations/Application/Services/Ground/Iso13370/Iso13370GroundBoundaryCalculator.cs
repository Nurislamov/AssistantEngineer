using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

public sealed class Iso13370GroundBoundaryCalculator
{
    private const double BaseCharacteristicDepthM = 1.5;
    private const double PerimeterAmplificationFactor = 0.9;

    private readonly Iso13370GroundTemperatureProfileCalculator _profileCalculator;

    public Iso13370GroundBoundaryCalculator(
        Iso13370GroundTemperatureProfileCalculator profileCalculator)
    {
        _profileCalculator = profileCalculator;
    }

    public Iso13370GroundBoundaryResult Calculate(Iso13370GroundBoundaryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<Iso13370GroundBoundaryDiagnostics>();

        var area = ClampMin(
            input.AreaM2,
            0.1,
            "Iso13370GroundBoundary.AreaClamped",
            "AreaM2 was clamped to the minimum positive value.",
            diagnostics);

        var exposedPerimeter = ClampMin(
            input.ExposedPerimeterM,
            0.1,
            "Iso13370GroundBoundary.PerimeterClamped",
            "ExposedPerimeterM was clamped to the minimum positive value.",
            diagnostics);

        var conductivity = ClampMin(
            input.GroundConductivityWPerMK,
            0.01,
            "Iso13370GroundBoundary.ConductivityClamped",
            "GroundConductivityWPerMK was clamped to the minimum positive value.",
            diagnostics);

        var floorU = ClampMin(
            input.FloorUValueWPerM2K,
            0.01,
            "Iso13370GroundBoundary.FloorUValueClamped",
            "FloorUValueWPerM2K was clamped to the minimum positive value.",
            diagnostics);

        var horizontalInsulationWidth = Math.Max(input.HorizontalInsulationWidthM, 0.0);
        var perimeterInsulationDepth = Math.Max(input.PerimeterInsulationDepthM, 0.0);
        var burialDepth = Math.Max(input.BurialDepthM, 0.0);
        var wallHeightBelowGrade = Math.Max(input.WallHeightBelowGradeM, 0.0);
        var underfloorVentilationAch = Math.Max(input.UnderfloorVentilationAirChangesPerHour, 0.0);

        var characteristicDimension = Math.Max(area / (0.5 * exposedPerimeter), 0.1);
        var insulationEffect = 1.0 - Math.Min(
            0.55,
            horizontalInsulationWidth * 0.12 + perimeterInsulationDepth * 0.08);
        var burialEffect = 1.0 + Math.Min(0.45, burialDepth * 0.12 + wallHeightBelowGrade * 0.08);
        var perimeterEffect = 1.0 + PerimeterAmplificationFactor / (1.0 + characteristicDimension);
        var contactFactor = GetContactFactor(input.ContactKind);
        var ventilationModifier = GetVentilationModifier(input.ContactKind, underfloorVentilationAch);

        var geometryEquivalentU =
            (conductivity / (characteristicDimension + BaseCharacteristicDepthM)) *
            perimeterEffect *
            burialEffect *
            insulationEffect *
            contactFactor *
            ventilationModifier;

        var equivalentU = Math.Max(0.01, 0.5 * geometryEquivalentU + 0.5 * floorU);
        var heatTransfer = Math.Max(0.01, equivalentU * area);

        var (groundWeight, outdoorWeight, indoorWeight) = GetBoundaryWeights(
            input.ContactKind,
            underfloorVentilationAch);

        var groundMonthly = _profileCalculator.BuildGroundMonthlyProfile(
            input.GroundAnnualMeanTemperatureC,
            input.GroundTemperatureAmplitudeC,
            input.GroundTemperaturePhaseShiftMonths);
        var outdoorMonthly = _profileCalculator.ResolveOutdoorMonthlyProfile(
            input.OutdoorMonthlyMeanTemperaturesC,
            input.OutdoorAnnualMeanTemperatureC);

        var profile = _profileCalculator.BuildBoundaryProfile(
            groundMonthly,
            outdoorMonthly,
            input.IndoorAnnualMeanTemperatureC,
            groundWeight,
            outdoorWeight,
            indoorWeight);

        diagnostics.Add(new Iso13370GroundBoundaryDiagnostics(
            "Iso13370GroundBoundary.Summary",
            $"Contact={input.ContactKind}, CharacteristicDimension={characteristicDimension:F6} m, EquivalentU={equivalentU:F6} W/m2K, HeatTransfer={heatTransfer:F6} W/K."));

        return new Iso13370GroundBoundaryResult(
            CharacteristicDimensionM: Round6(characteristicDimension),
            EquivalentGroundUValueWPerM2K: Round6(equivalentU),
            HeatTransferCoefficientWPerK: Round6(heatTransfer),
            GroundWeight: Round6(groundWeight),
            OutdoorWeight: Round6(outdoorWeight),
            IndoorWeight: Round6(indoorWeight),
            MonthlyBoundaryTemperaturesC: profile.MonthlyRecords.Select(item => item.BoundaryTemperatureC).ToArray(),
            AnnualMeanBoundaryTemperatureC: profile.AnnualMeanBoundaryTemperatureC,
            TemperatureProfile: profile,
            Diagnostics: diagnostics);
    }

    private static double GetContactFactor(Iso13370GroundContactKind kind) => kind switch
    {
        Iso13370GroundContactKind.SlabOnGround => 1.0,
        Iso13370GroundContactKind.ConditionedBasement => 0.65,
        Iso13370GroundContactKind.UnconditionedBasement => 0.80,
        Iso13370GroundContactKind.CrawlSpace => 0.75,
        Iso13370GroundContactKind.VentilatedCrawlSpace => 0.95,
        _ => 1.0
    };

    private static double GetVentilationModifier(
        Iso13370GroundContactKind kind,
        double underfloorVentilationAch) => kind switch
    {
        Iso13370GroundContactKind.CrawlSpace => 1.0 + Math.Min(0.15, underfloorVentilationAch * 0.03),
        Iso13370GroundContactKind.VentilatedCrawlSpace => 1.0 + Math.Min(0.35, underfloorVentilationAch * 0.05),
        _ => 1.0
    };

    private static (double ground, double outdoor, double indoor) GetBoundaryWeights(
        Iso13370GroundContactKind kind,
        double underfloorVentilationAch)
    {
        return kind switch
        {
            Iso13370GroundContactKind.SlabOnGround => (0.90, 0.10, 0.0),
            Iso13370GroundContactKind.ConditionedBasement => (0.35, 0.0, 0.65),
            Iso13370GroundContactKind.UnconditionedBasement => (0.50, 0.50, 0.0),
            Iso13370GroundContactKind.CrawlSpace =>
                (Math.Max(0.45, 0.75 - underfloorVentilationAch * 0.03),
                    Math.Min(0.55, 0.25 + underfloorVentilationAch * 0.03),
                    0.0),
            Iso13370GroundContactKind.VentilatedCrawlSpace =>
                (Math.Max(0.15, 0.35 - underfloorVentilationAch * 0.04),
                    Math.Min(0.85, 0.65 + underfloorVentilationAch * 0.04),
                    0.0),
            _ => (1.0, 0.0, 0.0)
        };
    }

    private static double ClampMin(
        double value,
        double minimum,
        string code,
        string message,
        ICollection<Iso13370GroundBoundaryDiagnostics> diagnostics)
    {
        if (value >= minimum)
            return value;

        diagnostics.Add(new Iso13370GroundBoundaryDiagnostics(code, message));
        return minimum;
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
