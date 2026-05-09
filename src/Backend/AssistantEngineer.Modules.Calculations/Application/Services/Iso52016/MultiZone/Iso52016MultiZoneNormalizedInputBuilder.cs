using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneNormalizedInputBuilder : IIso52016MultiZoneNormalizedInputBuilder
{
    private static readonly IReadOnlyList<string> DefaultClaimFlags =
    [
        "validation anchor",
        "internal engineering anchor",
        "standard-based calculation",
        "not full validation"
    ];

    private readonly IThermalBoundaryClassificationService _classificationService;
    private readonly IAdjacentUnconditionedZoneTemperatureCalculator _adjacentTemperatureCalculator;

    public Iso52016MultiZoneNormalizedInputBuilder(
        IThermalBoundaryClassificationService classificationService,
        IAdjacentUnconditionedZoneTemperatureCalculator adjacentTemperatureCalculator)
    {
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
        _adjacentTemperatureCalculator = adjacentTemperatureCalculator ?? throw new ArgumentNullException(nameof(adjacentTemperatureCalculator));
    }

    public MultiZoneNormalizedInputBuildResult Build(MultiZoneNormalizedInputBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(request.BuildingId))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.NormalizedInput.BuildingIdMissing",
                "Building id is required for multi-zone normalized input build."));
        }

        var classification = _classificationService.Classify(new ThermalBoundaryClassificationRequest(
            Zones: request.Zones ?? [],
            AllowUnknownExposure: request.AllowUnknownExposure,
            TreatSameUseAdjacentAsAdiabatic: request.TreatSameUseAdjacentAsAdiabatic,
            SameUseAdjacentConductanceFactor: request.SameUseAdjacentConductanceFactor));
        diagnostics.AddRange(classification.Diagnostics);

        var zoneProfilesByZoneId = (request.ZoneHourlyProfiles ?? [])
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ZoneId))
            .GroupBy(profile => profile.ZoneId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var normalizedBoundariesByZone = classification.Boundaries
            .GroupBy(boundary => boundary.SourceZoneId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<NormalizedThermalBoundary>)group
                    .OrderBy(item => item.BoundaryId, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var zones = (request.Zones ?? [])
            .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
            .Select(zone =>
            {
                if (!zoneProfilesByZoneId.ContainsKey(zone.ZoneId))
                {
                    diagnostics.Add(CreateError(
                        "Iso52016.MultiZone.NormalizedInput.ZoneHourlyProfileMissing",
                        $"Zone '{zone.ZoneId}' has no hourly profile."));
                }

                var boundaryIds = normalizedBoundariesByZone.TryGetValue(zone.ZoneId, out var zoneBoundaries)
                    ? zoneBoundaries.Select(boundary => boundary.BoundaryId).ToArray()
                    : [];

                return new ThermalZoneNode(
                    ZoneId: zone.ZoneId,
                    Name: zone.Name,
                    FloorAreaSquareMeters: zone.FloorAreaSquareMeters,
                    VolumeCubicMeters: zone.VolumeCubicMeters,
                    BoundaryIds: boundaryIds);
            })
            .ToArray();

        var exteriorProfile = request.ExteriorTemperatureProfileCelsius ?? [];
        if (exteriorProfile.Count is not (1 or 8760))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.NormalizedInput.ExteriorProfileLengthUnsupported",
                $"Exterior profile must have 1 or 8760 values, but got {exteriorProfile.Count}."));
        }

        var hourlyBoundaryConditions = new List<MultiZoneHourlyBoundaryCondition>();
        var boundaryLinks = new List<ThermalZoneBoundaryLink>();
        var interZonePairKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var boundary in classification.Boundaries
                     .OrderBy(item => item.SourceZoneId, StringComparer.Ordinal)
                     .ThenBy(item => item.BoundaryId, StringComparer.Ordinal))
        {
            if (boundary.IsAdiabaticEquivalent &&
                boundary.ExposureKind != BoundaryExposureKind.SameUseAdjacentZone)
            {
                continue;
            }

            var boundaryType = MapBoundaryType(boundary.ExposureKind);
            switch (boundaryType)
            {
                case MultiZoneBoundaryLinkType.ExternalBoundary:
                    {
                        hourlyBoundaryConditions.Add(new MultiZoneHourlyBoundaryCondition(
                            BoundaryId: boundary.BoundaryId,
                            TemperatureProfileCelsius: exteriorProfile));

                        boundaryLinks.Add(new ThermalZoneBoundaryLink(
                            LinkId: $"LINK-{boundary.BoundaryId}",
                            BoundaryType: boundaryType,
                            SourceZoneId: boundary.SourceZoneId,
                            SourceBoundaryId: boundary.BoundaryId,
                            AreaSquareMeters: boundary.AreaSquareMeters,
                            ConductanceWPerK: boundary.ConductanceWPerKelvin));
                        break;
                    }

                case MultiZoneBoundaryLinkType.GroundBoundary:
                    {
                        var groundProfile = ResolveGroundProfile(boundary, request, diagnostics);
                        hourlyBoundaryConditions.Add(new MultiZoneHourlyBoundaryCondition(
                            BoundaryId: boundary.BoundaryId,
                            TemperatureProfileCelsius: groundProfile));

                        boundaryLinks.Add(new ThermalZoneBoundaryLink(
                            LinkId: $"LINK-{boundary.BoundaryId}",
                            BoundaryType: boundaryType,
                            SourceZoneId: boundary.SourceZoneId,
                            SourceBoundaryId: boundary.BoundaryId,
                            AreaSquareMeters: boundary.AreaSquareMeters,
                            ConductanceWPerK: boundary.ConductanceWPerKelvin));
                        break;
                    }

                case MultiZoneBoundaryLinkType.AdjacentUnconditionedZone:
                    {
                        var adjacentProfile = ResolveAdjacentUnconditionedProfile(boundary, request, zoneProfilesByZoneId, diagnostics);
                        if (adjacentProfile.Count == 0)
                            continue;

                        boundaryLinks.Add(new ThermalZoneBoundaryLink(
                            LinkId: $"LINK-{boundary.BoundaryId}",
                            BoundaryType: boundaryType,
                            SourceZoneId: boundary.SourceZoneId,
                            SourceBoundaryId: boundary.BoundaryId,
                            AreaSquareMeters: boundary.AreaSquareMeters,
                            ConductanceWPerK: boundary.ConductanceWPerKelvin,
                            AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                                ConditionId: $"ADJ-UNCOND-{boundary.BoundaryId}",
                                TemperatureProfileCelsius: adjacentProfile,
                                IsAdiabaticEquivalent: false,
                                Notes: "Simplified adjacent unconditioned zone temperature lane for internal engineering calculations.")));
                        break;
                    }

                case MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone:
                    {
                        var isAdiabatic = boundary.IsAdiabaticEquivalent || request.TreatSameUseAdjacentAsAdiabatic;
                        boundaryLinks.Add(new ThermalZoneBoundaryLink(
                            LinkId: $"LINK-{boundary.BoundaryId}",
                            BoundaryType: boundaryType,
                            SourceZoneId: boundary.SourceZoneId,
                            SourceBoundaryId: boundary.BoundaryId,
                            AreaSquareMeters: boundary.AreaSquareMeters,
                            ConductanceWPerK: isAdiabatic ? 0.0 : boundary.ConductanceWPerKelvin,
                            TargetZoneId: boundary.AdjacentZoneId,
                            AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                                ConditionId: $"SAME-USE-{boundary.BoundaryId}",
                                TemperatureProfileCelsius: ResolveProfileFromZone(boundary.SourceZoneId, zoneProfilesByZoneId),
                                IsAdiabaticEquivalent: isAdiabatic,
                                Notes: "Same-use adjacent boundary treated with simplified adiabatic-style policy.")));
                        break;
                    }

                case MultiZoneBoundaryLinkType.InterZoneBoundary:
                    {
                        if (string.IsNullOrWhiteSpace(boundary.AdjacentZoneId))
                            continue;

                        var pairKey = BuildPairKey(boundary.SourceZoneId, boundary.AdjacentZoneId);
                        if (!interZonePairKeys.Add(pairKey))
                        {
                            diagnostics.Add(CreateWarning(
                                "Iso52016.MultiZone.NormalizedInput.InterZonePairDeduplicated",
                                $"Inter-zone pair '{pairKey}' was deduplicated to avoid double counting."));
                            continue;
                        }

                        boundaryLinks.Add(new ThermalZoneBoundaryLink(
                            LinkId: $"LINK-{boundary.BoundaryId}",
                            BoundaryType: boundaryType,
                            SourceZoneId: boundary.SourceZoneId,
                            SourceBoundaryId: boundary.BoundaryId,
                            AreaSquareMeters: boundary.AreaSquareMeters,
                            ConductanceWPerK: boundary.ConductanceWPerKelvin,
                            TargetZoneId: boundary.AdjacentZoneId));
                        break;
                    }
            }
        }

        var orderedHourlyBoundaryConditions = hourlyBoundaryConditions
            .GroupBy(condition => condition.BoundaryId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(condition => condition.BoundaryId, StringComparer.Ordinal)
            .ToArray();

        var orderedBoundaryLinks = boundaryLinks
            .OrderBy(link => link.LinkId, StringComparer.Ordinal)
            .ToArray();

        var orderedProfiles = zones
            .Where(zone => zoneProfilesByZoneId.ContainsKey(zone.ZoneId))
            .Select(zone => zoneProfilesByZoneId[zone.ZoneId])
            .ToArray();

        var claimFlags = request.ClaimFlags is { Count: > 0 }
            ? request.ClaimFlags.ToArray()
            : DefaultClaimFlags;

        var input = new MultiZoneCalculationInput(
            BuildingId: request.BuildingId,
            Zones: zones,
            BoundaryLinks: orderedBoundaryLinks,
            InterZoneConductanceLinks: [],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions: orderedHourlyBoundaryConditions,
            ZoneHourlyProfiles: orderedProfiles,
            ClaimFlags: claimFlags);

        var orderedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new MultiZoneNormalizedInputBuildResult(
            Input: input,
            Diagnostics: orderedDiagnostics);
    }

    private static MultiZoneBoundaryLinkType MapBoundaryType(BoundaryExposureKind exposureKind) =>
        exposureKind switch
        {
            BoundaryExposureKind.ExteriorAir => MultiZoneBoundaryLinkType.ExternalBoundary,
            BoundaryExposureKind.Ground => MultiZoneBoundaryLinkType.GroundBoundary,
            BoundaryExposureKind.AdjacentConditionedZone => MultiZoneBoundaryLinkType.InterZoneBoundary,
            BoundaryExposureKind.AdjacentUnconditionedZone => MultiZoneBoundaryLinkType.AdjacentUnconditionedZone,
            BoundaryExposureKind.SameUseAdjacentZone => MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone,
            _ => MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone
        };

    private static IReadOnlyList<double> ResolveGroundProfile(
        NormalizedThermalBoundary boundary,
        MultiZoneNormalizedInputBuildRequest request,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (request.GroundTemperatureProfilesByBoundaryId is not null &&
            request.GroundTemperatureProfilesByBoundaryId.TryGetValue(boundary.BoundaryId, out var groundProfile) &&
            groundProfile.Count > 0)
        {
            return groundProfile;
        }

        diagnostics.Add(CreateWarning(
            "Iso52016.MultiZone.NormalizedInput.GroundProfileFallbackToExterior",
            $"Ground boundary '{boundary.BoundaryId}' has no explicit ground profile and falls back to exterior profile."));
        return request.ExteriorTemperatureProfileCelsius;
    }

    private IReadOnlyList<double> ResolveAdjacentUnconditionedProfile(
        NormalizedThermalBoundary boundary,
        MultiZoneNormalizedInputBuildRequest request,
        IReadOnlyDictionary<string, MultiZoneZoneHourlyProfile> zoneProfilesByZoneId,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (request.AdjacentUnconditionedTemperatureProfilesByBoundaryId is not null &&
            request.AdjacentUnconditionedTemperatureProfilesByBoundaryId.TryGetValue(boundary.BoundaryId, out var profile) &&
            profile.Count > 0)
        {
            return profile;
        }

        if (!zoneProfilesByZoneId.TryGetValue(boundary.SourceZoneId, out var sourceZoneProfile))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.NormalizedInput.AdjacentUnconditionedSourceZoneProfileMissing",
                $"Adjacent-unconditioned boundary '{boundary.BoundaryId}' cannot resolve source zone profile '{boundary.SourceZoneId}'."));
            return [];
        }

        var mode = request.AdjacentUnconditionedReductionFactorByBoundaryId is not null &&
                   request.AdjacentUnconditionedReductionFactorByBoundaryId.TryGetValue(boundary.BoundaryId, out _)
            ? AdjacentUnconditionedTemperatureMode.ReductionFactor
            : AdjacentUnconditionedTemperatureMode.DeterministicFallback;

        var reductionFactor = request.AdjacentUnconditionedReductionFactorByBoundaryId is not null &&
                              request.AdjacentUnconditionedReductionFactorByBoundaryId.TryGetValue(boundary.BoundaryId, out var factor)
            ? (double?)factor
            : null;

        var calculation = _adjacentTemperatureCalculator.Calculate(
            new AdjacentUnconditionedZoneTemperatureProfileRequest(
                ConditionId: $"ADJ-UNCOND-{boundary.BoundaryId}",
                ConditionedZoneTemperatureProfileCelsius: sourceZoneProfile.HeatingSetpointProfileCelsius,
                ExteriorTemperatureProfileCelsius: request.ExteriorTemperatureProfileCelsius,
                Mode: mode,
                ReductionFactorB: reductionFactor,
                FallbackExteriorWeight: request.AdjacentUnconditionedFallbackExteriorWeight,
                FallbackOffsetCelsius: request.AdjacentUnconditionedFallbackOffsetCelsius));

        foreach (var diagnostic in calculation.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }
        if (mode == AdjacentUnconditionedTemperatureMode.DeterministicFallback)
        {
            diagnostics.Add(CreateWarning(
                "Iso52016.MultiZone.NormalizedInput.AdjacentUnconditionedDeterministicFallbackApplied",
                $"Boundary '{boundary.BoundaryId}' used deterministic fallback adjacent-unconditioned lane."));
        }

        return calculation.TemperatureProfileCelsius;
    }

    private static IReadOnlyList<double> ResolveProfileFromZone(
        string zoneId,
        IReadOnlyDictionary<string, MultiZoneZoneHourlyProfile> zoneProfilesByZoneId)
    {
        if (zoneProfilesByZoneId.TryGetValue(zoneId, out var profile))
            return profile.HeatingSetpointProfileCelsius;

        return [20.0];
    }

    private static string BuildPairKey(
        string left,
        string right) =>
        string.Compare(left, right, StringComparison.Ordinal) <= 0
            ? $"{left}<->{right}"
            : $"{right}<->{left}";

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneNormalizedInputBuilder",
            Source: "Iso52016MultiZoneNormalizedInputBuilder",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.InputPreparation);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Warning,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneNormalizedInputBuilder",
            Source: "Iso52016MultiZoneNormalizedInputBuilder",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.InputPreparation);
}
