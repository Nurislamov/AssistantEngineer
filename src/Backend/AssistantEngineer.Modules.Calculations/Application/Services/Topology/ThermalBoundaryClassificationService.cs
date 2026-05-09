using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Topology;

public sealed class ThermalBoundaryClassificationService : IThermalBoundaryClassificationService
{
    public ThermalBoundaryClassificationResult Classify(ThermalBoundaryClassificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var zones = request.Zones ?? [];
        var zoneIdSet = zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ZoneId))
            .Select(zone => zone.ZoneId)
            .ToHashSet(StringComparer.Ordinal);

        ValidateZones(zones, diagnostics);

        var normalizedBoundaries = new List<NormalizedThermalBoundary>();
        foreach (var zone in zones.OrderBy(item => item.ZoneId, StringComparer.Ordinal))
        {
            foreach (var boundary in (zone.Boundaries ?? [])
                         .OrderBy(item => item.BoundaryId, StringComparer.Ordinal))
            {
                NormalizeBoundary(
                    zone,
                    boundary,
                    zoneIdSet,
                    request,
                    normalizedBoundaries,
                    diagnostics);
            }
        }

        ValidateInterZonePairs(normalizedBoundaries, diagnostics);

        var orderedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new ThermalBoundaryClassificationResult(
            Zones: zones,
            Boundaries: normalizedBoundaries,
            Diagnostics: orderedDiagnostics);
    }

    private static void ValidateZones(
        IReadOnlyList<ThermalZoneDefinition> zones,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        foreach (var duplicateZoneId in zones
                     .Select(zone => zone.ZoneId)
                     .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId))
                     .GroupBy(zoneId => zoneId, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.ZoneIdDuplicate",
                $"Thermal zone id '{duplicateZoneId}' is duplicated."));
        }

        foreach (var zone in zones)
        {
            if (string.IsNullOrWhiteSpace(zone.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "Topology.Classification.ZoneIdMissing",
                    "Thermal zone id is required."));
            }

            if (!(zone.FloorAreaSquareMeters > 0.0))
            {
                diagnostics.Add(CreateError(
                    "Topology.Classification.ZoneAreaNonPositive",
                    $"Thermal zone '{zone.ZoneId}' floor area must be greater than zero."));
            }

            if (!(zone.VolumeCubicMeters > 0.0))
            {
                diagnostics.Add(CreateError(
                    "Topology.Classification.ZoneVolumeNonPositive",
                    $"Thermal zone '{zone.ZoneId}' volume must be greater than zero."));
            }
        }
    }

    private static void NormalizeBoundary(
        ThermalZoneDefinition zone,
        ThermalBoundaryDefinition boundary,
        ISet<string> zoneIdSet,
        ThermalBoundaryClassificationRequest request,
        ICollection<NormalizedThermalBoundary> normalizedBoundaries,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!string.Equals(boundary.SourceZoneId, zone.ZoneId, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundarySourceMismatch",
                $"Boundary '{boundary.BoundaryId}' source zone '{boundary.SourceZoneId}' does not match owner zone '{zone.ZoneId}'."));
        }

        if (string.IsNullOrWhiteSpace(boundary.BoundaryId))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryIdMissing",
                $"Zone '{zone.ZoneId}' contains a boundary with missing id."));
            return;
        }

        if (!(boundary.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryAreaNonPositive",
                $"Boundary '{boundary.BoundaryId}' area must be greater than zero."));
            return;
        }

        if (boundary.UValueWPerSquareMeterKelvin.HasValue &&
            !(boundary.UValueWPerSquareMeterKelvin.Value > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryUValueNonPositive",
                $"Boundary '{boundary.BoundaryId}' U-value must be greater than zero when provided."));
            return;
        }

        if (boundary.ConductanceWPerKelvin.HasValue &&
            !(boundary.ConductanceWPerKelvin.Value > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryConductanceNonPositive",
                $"Boundary '{boundary.BoundaryId}' conductance must be greater than zero when provided."));
            return;
        }

        if (string.Equals(boundary.AdjacentZoneId, boundary.SourceZoneId, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundarySelfReference",
                $"Boundary '{boundary.BoundaryId}' cannot reference the same source/adjacent zone '{boundary.SourceZoneId}'."));
            return;
        }

        var hasAdjacentZone = !string.IsNullOrWhiteSpace(boundary.AdjacentZoneId);
        var isUnknownExposure = boundary.ExposureKind == BoundaryExposureKind.Unknown;
        if (isUnknownExposure && !request.AllowUnknownExposure)
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.UnknownExposureBlocked",
                $"Boundary '{boundary.BoundaryId}' uses Unknown exposure, which is disabled by validation mode."));
            return;
        }

        if (isUnknownExposure)
        {
            diagnostics.Add(CreateWarning(
                "Topology.Classification.UnknownExposureAllowed",
                $"Boundary '{boundary.BoundaryId}' uses Unknown exposure in permissive validation mode."));
        }

        if (hasAdjacentZone && !zoneIdSet.Contains(boundary.AdjacentZoneId!))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.AdjacentZoneMissing",
                $"Boundary '{boundary.BoundaryId}' references adjacent zone '{boundary.AdjacentZoneId}', which is missing."));
            return;
        }

        var exposure = boundary.ExposureKind;
        switch (exposure)
        {
            case BoundaryExposureKind.ExteriorAir:
                if (hasAdjacentZone)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Classification.ExteriorBoundaryAdjacentZoneForbidden",
                        $"Boundary '{boundary.BoundaryId}' is exterior and cannot specify adjacent zone '{boundary.AdjacentZoneId}'."));
                    return;
                }

                break;

            case BoundaryExposureKind.Ground:
                if (hasAdjacentZone)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Classification.GroundBoundaryAdjacentZoneForbidden",
                        $"Boundary '{boundary.BoundaryId}' is ground and cannot specify adjacent zone '{boundary.AdjacentZoneId}'."));
                    return;
                }

                break;

            case BoundaryExposureKind.AdjacentConditionedZone:
            case BoundaryExposureKind.AdjacentUnconditionedZone:
            case BoundaryExposureKind.SameUseAdjacentZone:
                if (!hasAdjacentZone)
                {
                    diagnostics.Add(CreateError(
                        "Topology.Classification.AdjacentBoundaryZoneMissing",
                        $"Boundary '{boundary.BoundaryId}' requires an adjacent zone reference."));
                    return;
                }

                break;

            case BoundaryExposureKind.Adiabatic:
            case BoundaryExposureKind.InternalMass:
                break;
        }

        if (request.RequireSolarMetadataForTransparentExteriorBoundaries &&
            boundary.IsTransparent == true &&
            exposure == BoundaryExposureKind.ExteriorAir &&
            (!boundary.OrientationDegrees.HasValue || !boundary.TiltDegrees.HasValue))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.TransparentSolarMetadataMissing",
                $"Boundary '{boundary.BoundaryId}' is transparent exterior and requires orientation/tilt metadata."));
            return;
        }

        var conductance = ResolveConductance(boundary, diagnostics);
        if (!conductance.HasValue)
            return;

        var isAdiabaticEquivalent = exposure is BoundaryExposureKind.Adiabatic or BoundaryExposureKind.InternalMass;

        if (exposure == BoundaryExposureKind.SameUseAdjacentZone)
        {
            if (request.TreatSameUseAdjacentAsAdiabatic)
            {
                conductance = 0.0;
                isAdiabaticEquivalent = true;
                diagnostics.Add(CreateInfo(
                    "Topology.Classification.SameUseAdjacentAdiabaticPolicyApplied",
                    $"Boundary '{boundary.BoundaryId}' is treated as adiabatic-style per simplified same-use policy."));
            }
            else
            {
                var factor = Math.Clamp(request.SameUseAdjacentConductanceFactor, 0.0, 1.0);
                conductance *= factor;
                diagnostics.Add(CreateInfo(
                    "Topology.Classification.SameUseAdjacentReducedConductancePolicyApplied",
                    $"Boundary '{boundary.BoundaryId}' uses reduced conductance factor {factor:0.###} under same-use policy."));
            }
        }

        if (isAdiabaticEquivalent)
            conductance = 0.0;

        normalizedBoundaries.Add(new NormalizedThermalBoundary(
            BoundaryId: boundary.BoundaryId,
            SourceZoneId: boundary.SourceZoneId,
            AdjacentZoneId: boundary.AdjacentZoneId,
            ExposureKind: exposure,
            ElementKind: boundary.ElementKind,
            AreaSquareMeters: boundary.AreaSquareMeters,
            ConductanceWPerKelvin: conductance.Value,
            IsTransparent: boundary.IsTransparent ?? boundary.ElementKind is BoundaryElementKind.Window or BoundaryElementKind.Door,
            IsAdiabaticEquivalent: isAdiabaticEquivalent,
            RequiresExteriorTemperature: exposure == BoundaryExposureKind.ExteriorAir,
            RequiresGroundTemperature: exposure == BoundaryExposureKind.Ground,
            RequiresAdjacentZoneTemperature: exposure is BoundaryExposureKind.AdjacentConditionedZone or BoundaryExposureKind.SameUseAdjacentZone,
            RequiresAdjacentUnconditionedTemperature: exposure == BoundaryExposureKind.AdjacentUnconditionedZone,
            OrientationDegrees: boundary.OrientationDegrees,
            TiltDegrees: boundary.TiltDegrees,
            Notes: boundary.Notes));
    }

    private static void ValidateInterZonePairs(
        IReadOnlyList<NormalizedThermalBoundary> boundaries,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var adjacencyBoundaries = boundaries
            .Where(boundary =>
                !string.IsNullOrWhiteSpace(boundary.AdjacentZoneId) &&
                boundary.ExposureKind is BoundaryExposureKind.AdjacentConditionedZone or BoundaryExposureKind.SameUseAdjacentZone)
            .ToArray();

        var duplicateGroups = adjacencyBoundaries
            .GroupBy(
                boundary => CreatePairKey(boundary.SourceZoneId, boundary.AdjacentZoneId!),
                StringComparer.Ordinal);

        foreach (var group in duplicateGroups)
        {
            var count = group.Count();
            if (count <= 1)
                continue;

            var groupItems = group.OrderBy(item => item.BoundaryId, StringComparer.Ordinal).ToArray();
            if (count > 2)
            {
                diagnostics.Add(CreateError(
                    "Topology.Classification.InterZonePairDuplicate",
                    $"Pair '{group.Key}' has {count} inter-zone boundaries and risks double counting."));
                continue;
            }

            if (string.Equals(groupItems[0].SourceZoneId, groupItems[1].SourceZoneId, StringComparison.Ordinal))
            {
                diagnostics.Add(CreateError(
                    "Topology.Classification.InterZoneDirectionalDuplicate",
                    $"Pair '{group.Key}' has duplicated same-direction inter-zone boundaries."));
            }

            if (Math.Abs(groupItems[0].AreaSquareMeters - groupItems[1].AreaSquareMeters) > 1e-6)
            {
                diagnostics.Add(CreateWarning(
                    "Topology.Classification.InterZonePairAreaMismatch",
                    $"Pair '{group.Key}' has inconsistent area values ({groupItems[0].AreaSquareMeters:0.###} vs {groupItems[1].AreaSquareMeters:0.###} m2)."));
            }

            if (Math.Abs(groupItems[0].ConductanceWPerKelvin - groupItems[1].ConductanceWPerKelvin) > 1e-6)
            {
                diagnostics.Add(CreateWarning(
                    "Topology.Classification.InterZonePairConductanceMismatch",
                    $"Pair '{group.Key}' has inconsistent conductance values ({groupItems[0].ConductanceWPerKelvin:0.###} vs {groupItems[1].ConductanceWPerKelvin:0.###} W/K)."));
            }
        }
    }

    private static string CreatePairKey(
        string left,
        string right) =>
        string.Compare(left, right, StringComparison.Ordinal) <= 0
            ? $"{left}<->{right}"
            : $"{right}<->{left}";

    private static double? ResolveConductance(
        ThermalBoundaryDefinition boundary,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (boundary.ConductanceWPerKelvin.HasValue)
            return boundary.ConductanceWPerKelvin.Value;

        if (!boundary.UValueWPerSquareMeterKelvin.HasValue)
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryConductanceInputMissing",
                $"Boundary '{boundary.BoundaryId}' requires either explicit conductance or U-value."));
            return null;
        }

        var conductance = boundary.AreaSquareMeters * boundary.UValueWPerSquareMeterKelvin.Value;
        if (!(conductance > 0.0))
        {
            diagnostics.Add(CreateError(
                "Topology.Classification.BoundaryConductanceDerivedNonPositive",
                $"Boundary '{boundary.BoundaryId}' has non-positive derived conductance."));
            return null;
        }

        return conductance;
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Error,
            code: code,
            message: message,
            context: "ThermalBoundaryClassificationService",
            stage: StandardCalculationStage.BoundaryCondition);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Warning,
            code: code,
            message: message,
            context: "ThermalBoundaryClassificationService",
            stage: StandardCalculationStage.BoundaryCondition);

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        ThermalTopologyDiagnosticsFactory.Create(
            severity: CalculationDiagnosticSeverity.Info,
            code: code,
            message: message,
            context: "ThermalBoundaryClassificationService",
            stage: StandardCalculationStage.BoundaryCondition);
}
