using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class BuildingGroundBoundaryCalculator : IBuildingGroundBoundaryCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private readonly IGroundBoundaryTopologyMapper _topologyMapper;
    private readonly IGroundBoundaryCalculator _groundBoundaryCalculator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public BuildingGroundBoundaryCalculator(
        IGroundBoundaryTopologyMapper topologyMapper,
        IGroundBoundaryCalculator groundBoundaryCalculator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _topologyMapper = topologyMapper ?? throw new ArgumentNullException(nameof(topologyMapper));
        _groundBoundaryCalculator = groundBoundaryCalculator ?? throw new ArgumentNullException(nameof(groundBoundaryCalculator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public BuildingGroundBoundaryCalculationResult Calculate(BuildingGroundBoundaryCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Topology);
        ArgumentNullException.ThrowIfNull(input.GroundSurfaceMetadataBySurfaceId);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Topology.Diagnostics);

        var topology = input.Topology;
        var metadataBySurfaceId = input.GroundSurfaceMetadataBySurfaceId;
        var groundSurfaces = topology.Surfaces
            .Where(surface => surface.BoundaryKind == ThermalBoundaryKind.Ground)
            .ToArray();

        if (groundSurfaces.Length == 0)
        {
            diagnostics.Add(CreateInfo(
                "AE-GROUND-NO-GROUND-SURFACES",
                $"Building '{topology.BuildingId}' contains no surfaces marked as ground.",
                StandardCalculationStage.InputPreparation));
        }

        var knownSurfaceIds = new HashSet<string>(
            topology.Surfaces
                .Where(surface => !string.IsNullOrWhiteSpace(surface.SurfaceId))
                .Select(surface => surface.SurfaceId),
            StringComparer.Ordinal);

        foreach (var metadataSurfaceId in metadataBySurfaceId.Keys)
        {
            if (string.IsNullOrWhiteSpace(metadataSurfaceId) || !knownSurfaceIds.Contains(metadataSurfaceId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-METADATA-SURFACE-NOT-FOUND",
                    $"Ground metadata references unknown topology surface id '{metadataSurfaceId}'.",
                    StandardCalculationStage.InputPreparation));
            }
        }

        var groundSurfaceResults = new List<GroundSurfaceBoundaryCalculationResult>();
        var heatTransferBySurfaceId = new Dictionary<string, double>(StringComparer.Ordinal);
        var hourlyBySurfaceId = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var monthlyBySurfaceId = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var totalHeatTransfer = 0.0;

        foreach (var surface in groundSurfaces)
        {
            if (!metadataBySurfaceId.TryGetValue(surface.SurfaceId, out var metadata))
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-SURFACE-METADATA-MISSING",
                    $"Ground surface '{surface.SurfaceId}' is missing metadata required for ground boundary calculation.",
                    StandardCalculationStage.InputPreparation));
                continue;
            }

            var mappedInput = _topologyMapper.Map(topology, surface, metadata) with
            {
                DisclosureOverride = input.DisclosureOverride
            };

            var groundResult = _groundBoundaryCalculator.Calculate(mappedInput);
            diagnostics.AddRange(groundResult.Diagnostics);

            var surfaceDiagnostics = new List<StandardCalculationDiagnostic>();
            surfaceDiagnostics.AddRange(surface.Diagnostics);
            surfaceDiagnostics.AddRange(metadata.Diagnostics);
            surfaceDiagnostics.AddRange(groundResult.Diagnostics);

            var surfaceResult = new GroundSurfaceBoundaryCalculationResult(
                SurfaceId: surface.SurfaceId,
                BuildingId: topology.BuildingId,
                ZoneId: surface.ZoneId,
                RoomId: surface.RoomId,
                ContactKind: groundResult.ContactKind,
                EquivalentUValueWPerSquareMeterKelvin: groundResult.EquivalentUValueWPerSquareMeterKelvin,
                HeatTransferCoefficientWPerKelvin: groundResult.HeatTransferCoefficientWPerKelvin,
                MonthlyGroundBoundaryTemperaturesCelsius: groundResult.MonthlyGroundBoundaryTemperaturesCelsius,
                HourlyGroundBoundaryTemperaturesCelsius: groundResult.HourlyGroundBoundaryTemperaturesCelsius,
                GroundResult: groundResult,
                Diagnostics: surfaceDiagnostics);
            groundSurfaceResults.Add(surfaceResult);

            if (groundResult.HeatTransferCoefficientWPerKelvin is { } hValue && double.IsFinite(hValue))
            {
                heatTransferBySurfaceId[surface.SurfaceId] = hValue;
                totalHeatTransfer += hValue;
            }

            if (groundResult.HourlyGroundBoundaryTemperaturesCelsius.Count == 8760)
            {
                hourlyBySurfaceId[surface.SurfaceId] = groundResult.HourlyGroundBoundaryTemperaturesCelsius;
            }

            if (groundResult.MonthlyGroundBoundaryTemperaturesCelsius.Count == 12)
            {
                monthlyBySurfaceId[surface.SurfaceId] = groundResult.MonthlyGroundBoundaryTemperaturesCelsius;
            }
        }

        diagnostics.Add(CreateInfo(
            "AE-GROUND-BUILDING-CALCULATION-COMPLETED",
            $"Ground boundary batch calculation completed for building '{topology.BuildingId}' with {groundSurfaceResults.Count} calculated surface result(s).",
            StandardCalculationStage.Aggregation));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateGroundIso13370Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new BuildingGroundBoundaryCalculationResult(
            BuildingId: topology.BuildingId,
            GroundSurfaces: groundSurfaceResults,
            SurfaceHeatTransferCoefficientsWPerKelvin: heatTransferBySurfaceId,
            SurfaceHourlyGroundTemperaturesCelsius: hourlyBySurfaceId,
            SurfaceMonthlyGroundTemperaturesCelsius: monthlyBySurfaceId,
            TotalGroundHeatTransferCoefficientWPerKelvin: totalHeatTransfer,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
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
        foreach (var requiredClaim in RequiredForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredClaim);
        }

        var removedAllowedClaims = new List<string>();
        var allowedClaims = (overrideBoundary.AllowedClaims ?? [])
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim =>
            {
                var containsForbidden = forbiddenClaims.Any(forbidden =>
                    claim.Contains(forbidden, StringComparison.Ordinal));
                if (containsForbidden)
                    removedAllowedClaims.Add(claim);

                return !containsForbidden;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (removedAllowedClaims.Count > 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-DISCLOSURE-OVERRIDE-SANITIZED",
                $"Disclosure override removed forbidden allowed-claim entries: {string.Join(", ", removedAllowedClaims)}.",
                StandardCalculationStage.Diagnostics));
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
        string message,
        StandardCalculationStage stage) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            stage,
            "BuildingGroundBoundaryCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message,
        StandardCalculationStage stage) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            stage,
            "BuildingGroundBoundaryCalculator");
}
