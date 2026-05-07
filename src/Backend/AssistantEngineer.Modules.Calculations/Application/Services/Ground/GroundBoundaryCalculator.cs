using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryCalculator : IGroundBoundaryCalculator
{
    private readonly IGroundGeometryNormalizer _geometryNormalizer;
    private readonly IGroundBoundaryInputValidator _inputValidator;
    private readonly IGroundTemperatureProfileProvider _temperatureProfileProvider;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public GroundBoundaryCalculator(
        IGroundGeometryNormalizer geometryNormalizer,
        IGroundBoundaryInputValidator inputValidator,
        IGroundTemperatureProfileProvider temperatureProfileProvider,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _geometryNormalizer = geometryNormalizer ?? throw new ArgumentNullException(nameof(geometryNormalizer));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _temperatureProfileProvider = temperatureProfileProvider ?? throw new ArgumentNullException(nameof(temperatureProfileProvider));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public GroundBoundaryCalculationResult Calculate(GroundBoundaryCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Geometry.Diagnostics);
        diagnostics.AddRange(input.Soil.Diagnostics);
        diagnostics.AddRange(input.Climate.Diagnostics);

        var validation = _inputValidator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var normalizedGeometry = _geometryNormalizer.Normalize(input.ContactKind, input.Geometry);
        diagnostics.AddRange(normalizedGeometry.Diagnostics);

        var profile = _temperatureProfileProvider.BuildProfile(input.Climate);
        diagnostics.AddRange(profile.Diagnostics);

        diagnostics.Add(CreateInfo(
            "AE-GROUND-CALCULATION-STANDARD-INSPIRED",
            "Ground boundary calculation uses deterministic ISO13370-inspired engineering assumptions."));

        var equivalentU = CalculateEquivalentUValue(
            input.ContactKind,
            normalizedGeometry,
            input.Soil,
            diagnostics);

        double? heatTransferCoefficient = null;
        if (equivalentU.HasValue && normalizedGeometry.AreaSquareMeters > 0.0)
        {
            heatTransferCoefficient = equivalentU.Value * normalizedGeometry.AreaSquareMeters;
        }

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateGroundIso13370Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new GroundBoundaryCalculationResult(
            BoundaryId: input.BoundaryId,
            BuildingId: input.BuildingId,
            ZoneId: input.ZoneId,
            RoomId: input.RoomId,
            SurfaceId: input.SurfaceId,
            ContactKind: input.ContactKind,
            EquivalentUValueWPerSquareMeterKelvin: equivalentU,
            HeatTransferCoefficientWPerKelvin: heatTransferCoefficient,
            CharacteristicDimensionMeters: normalizedGeometry.CharacteristicDimensionMeters,
            MonthlyGroundBoundaryTemperaturesCelsius: profile.MonthlyGroundBoundaryTemperaturesCelsius,
            HourlyGroundBoundaryTemperaturesCelsius: profile.HourlyGroundBoundaryTemperaturesCelsius,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static double? CalculateEquivalentUValue(
        GroundContactKind contactKind,
        GroundContactGeometry geometry,
        GroundSoilProperties soil,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var area = geometry.AreaSquareMeters;
        if (!(area > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                "Equivalent U-value cannot be calculated because area is non-positive."));
            return null;
        }

        var insulationFactor = ComputeInsulationFactor(geometry, diagnostics);

        switch (contactKind)
        {
            case GroundContactKind.SlabOnGround:
                {
                    if (!(geometry.FloorUValueWPerSquareMeterKelvin > 0.0))
                    {
                        diagnostics.Add(CreateError(
                            "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                            "Slab-on-ground equivalent U-value requires floor U-value."));
                        return null;
                    }

                    var equivalentU = geometry.FloorUValueWPerSquareMeterKelvin.Value;
                    if (geometry.ExposedPerimeterMeters is > 0.0 &&
                        geometry.CharacteristicDimensionMeters is > 0.0 &&
                        soil.ConductivityWPerMeterKelvin > 0.0)
                    {
                        var perimeterRatio = geometry.ExposedPerimeterMeters.Value / Math.Max(area, 0.1);
                        var shapeFactor = 1.0 + Math.Min(0.25, perimeterRatio * 0.5);
                        var soilFactor = 1.0 + Math.Min(0.20, soil.ConductivityWPerMeterKelvin / 10.0);
                        var dimensionFactor = 1.0 + Math.Min(0.10, 1.0 / (1.0 + geometry.CharacteristicDimensionMeters.Value));
                        equivalentU *= shapeFactor * soilFactor * dimensionFactor;
                        diagnostics.Add(CreateInfo(
                            "AE-GROUND-PERIMETER-CORRECTION-APPLIED",
                            "Perimeter and characteristic-dimension correction was applied for slab-on-ground equivalent U-value."));
                    }
                    else
                    {
                        diagnostics.Add(CreateWarning(
                            "AE-GROUND-PERIMETER-CORRECTION-SKIPPED",
                            "Perimeter correction was skipped because characteristic-dimension/perimeter/soil metadata is incomplete."));
                    }

                    equivalentU *= insulationFactor;
                    diagnostics.Add(CreateInfo(
                        "AE-GROUND-EQUIVALENT-U-CALCULATED",
                        "Equivalent U-value was calculated for slab-on-ground contact kind."));
                    return equivalentU;
                }

            case GroundContactKind.SuspendedFloor:
            case GroundContactKind.Crawlspace:
                {
                    if (!(geometry.FloorUValueWPerSquareMeterKelvin > 0.0))
                    {
                        diagnostics.Add(CreateError(
                            "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                            "Suspended-floor/crawlspace equivalent U-value requires floor U-value."));
                        return null;
                    }

                    var crawlspaceFactor = contactKind == GroundContactKind.Crawlspace ? 0.95 : 1.05;
                    var equivalentU = geometry.FloorUValueWPerSquareMeterKelvin.Value * crawlspaceFactor * insulationFactor;
                    diagnostics.Add(CreateInfo(
                        "AE-GROUND-EQUIVALENT-U-CALCULATED",
                        "Equivalent U-value was calculated for suspended-floor/crawlspace contact kind."));
                    return equivalentU;
                }

            case GroundContactKind.HeatedBasement:
            case GroundContactKind.UnheatedBasement:
                {
                    var floorU = geometry.FloorUValueWPerSquareMeterKelvin;
                    var wallU = geometry.WallUValueWPerSquareMeterKelvin;
                    var perimeter = geometry.ExposedPerimeterMeters;
                    var wallHeight = geometry.BasementWallHeightMeters;

                    if (floorU is > 0.0 && wallU is > 0.0 && perimeter is > 0.0 && wallHeight is > 0.0)
                    {
                        var wallArea = perimeter.Value * wallHeight.Value;
                        var combinedH = floorU.Value * area + wallU.Value * wallArea;
                        var combinedU = combinedH / Math.Max(area + wallArea, 0.1);
                        var basementFactor = contactKind == GroundContactKind.HeatedBasement ? 0.90 : 1.05;
                        var equivalentU = combinedU * basementFactor * insulationFactor;

                        diagnostics.Add(CreateInfo(
                            "AE-GROUND-BASEMENT-COMBINED-H-USED",
                            "Basement floor and wall contributions were combined to produce equivalent U-value."));
                        diagnostics.Add(CreateInfo(
                            "AE-GROUND-EQUIVALENT-U-CALCULATED",
                            "Equivalent U-value was calculated for basement contact kind."));
                        return equivalentU;
                    }

                    if (floorU is > 0.0)
                    {
                        var fallbackFactor = contactKind == GroundContactKind.HeatedBasement ? 0.95 : 1.05;
                        var equivalentU = floorU.Value * fallbackFactor * insulationFactor;
                        diagnostics.Add(CreateWarning(
                            "AE-GROUND-BASEMENT-COMBINED-H-SKIPPED",
                            "Basement wall metadata is incomplete; floor-only fallback was used for equivalent U-value."));
                        diagnostics.Add(CreateInfo(
                            "AE-GROUND-EQUIVALENT-U-CALCULATED",
                            "Equivalent U-value was calculated using basement fallback."));
                        return equivalentU;
                    }

                    diagnostics.Add(CreateError(
                        "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                        "Basement equivalent U-value could not be calculated due to missing floor/wall U-value metadata."));
                    return null;
                }

            case GroundContactKind.BuriedWall:
                {
                    if (!(geometry.WallUValueWPerSquareMeterKelvin > 0.0))
                    {
                        diagnostics.Add(CreateError(
                            "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                            "Buried-wall equivalent U-value requires wall U-value."));
                        return null;
                    }

                    var depth = geometry.DepthBelowGroundMeters ?? 0.0;
                    var depthFactor = 1.0 + Math.Min(0.25, Math.Max(0.0, depth) * 0.05);
                    var equivalentU = geometry.WallUValueWPerSquareMeterKelvin.Value * depthFactor * insulationFactor;
                    diagnostics.Add(CreateInfo(
                        "AE-GROUND-EQUIVALENT-U-CALCULATED",
                        "Equivalent U-value was calculated for buried-wall contact kind."));
                    return equivalentU;
                }

            case GroundContactKind.Other:
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-OTHER-CONTACT-UNSUPPORTED",
                    "Contact kind Other is unsupported for deterministic equivalent U-value calculation."));
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                    "Equivalent U-value was not calculated for contact kind Other."));
                return null;

            default:
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-EQUIVALENT-U-NOT-CALCULABLE",
                    $"Equivalent U-value was not calculated for contact kind '{contactKind}'."));
                return null;
        }
    }

    private static double ComputeInsulationFactor(
        GroundContactGeometry geometry,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (geometry.InsulationPlacement == GroundInsulationPlacement.None)
            return 1.0;

        if (geometry.EdgeInsulationThicknessMeters is not > 0.0 ||
            geometry.EdgeInsulationConductivityWPerMeterKelvin is not > 0.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-INSULATION-INCOMPLETE",
                "Insulation metadata is incomplete; equivalent U-value uses neutral insulation factor."));
            return 1.0;
        }

        var resistance = geometry.EdgeInsulationThicknessMeters.Value / geometry.EdgeInsulationConductivityWPerMeterKelvin.Value;
        var reduction = Math.Min(0.35, resistance * 0.15);
        return 1.0 - reduction;
    }

    private static StandardCalculationDisclosure MergeDisclosure(
        StandardCalculationDisclosure baseDisclosure,
        StandardCalculationDisclosure? disclosureOverride,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (disclosureOverride is null)
            return baseDisclosure;

        var baseBoundary = baseDisclosure.ClaimBoundary;
        var overrideBoundary = disclosureOverride.ClaimBoundary ?? baseBoundary;

        var forbiddenClaims = overrideBoundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var requiredClaim in baseBoundary.ForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredClaim);
        }

        var removedAllowedClaims = new List<string>();
        var allowedClaims = (overrideBoundary.AllowedClaims ?? [])
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim =>
            {
                var isForbidden = forbiddenClaims.Contains(claim, StringComparer.Ordinal);
                if (isForbidden)
                    removedAllowedClaims.Add(claim);
                return !isForbidden;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (removedAllowedClaims.Count > 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-DISCLOSURE-OVERRIDE-SANITIZED",
                $"Disclosure override removed forbidden allowed-claim entries: {string.Join(", ", removedAllowedClaims)}."));
        }

        var mergedBoundary = new StandardClaimBoundary(
            AllowedClaims: allowedClaims,
            ForbiddenClaims: forbiddenClaims,
            Limitations: overrideBoundary.Limitations ?? baseBoundary.Limitations,
            Assumptions: overrideBoundary.Assumptions ?? baseBoundary.Assumptions);

        return disclosureOverride with
        {
            CalculationPath = string.IsNullOrWhiteSpace(disclosureOverride.CalculationPath)
                ? baseDisclosure.CalculationPath
                : disclosureOverride.CalculationPath,
            ClaimBoundary = mergedBoundary
        };
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryCalculator");

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryCalculator");
}
