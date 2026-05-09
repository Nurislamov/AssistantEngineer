using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneInputValidator : IIso52016MultiZoneInputValidator
{
    private static readonly HashSet<string> SupportedClaimFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "validation anchor",
        "internal engineering anchor",
        "standard-based calculation",
        "not full validation"
    };

    public MultiZoneInputValidationResult Validate(MultiZoneCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var zones = input.Zones ?? [];
        var boundaryLinks = input.BoundaryLinks ?? [];
        var interZoneConductanceLinks = input.InterZoneConductanceLinks ?? [];
        var interZoneAirflowLinks = input.InterZoneAirflowLinks ?? [];
        var hourlyBoundaryConditions = input.HourlyBoundaryConditions ?? [];
        var zoneHourlyProfiles = input.ZoneHourlyProfiles ?? [];
        var claimFlags = input.ClaimFlags ?? [];

        if (zones.Count == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.ZoneRequired",
                "Multi-zone calculation input must contain at least one zone."));
        }

        var duplicateZoneIds = zones
            .Select(zone => zone.ZoneId?.Trim() ?? string.Empty)
            .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .GroupBy(zoneId => zoneId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateZoneId in duplicateZoneIds)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.DuplicateZoneId",
                $"Zone id '{duplicateZoneId}' is duplicated."));
        }

        var zoneIdSet = zones
            .Select(zone => zone.ZoneId?.Trim() ?? string.Empty)
            .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var zone in zones)
        {
            if (string.IsNullOrWhiteSpace(zone.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneIdRequired",
                    "Zone id is required for all multi-zone graph nodes."));
            }

            if (zone.FloorAreaSquareMeters < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneAreaNegative",
                    $"Zone '{zone.ZoneId}' floor area cannot be negative."));
            }

            if (zone.VolumeCubicMeters < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneVolumeNegative",
                    $"Zone '{zone.ZoneId}' volume cannot be negative."));
            }
        }

        if (zoneHourlyProfiles.Count == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.ZoneHourlyProfilesRequired",
                "Multi-zone calculation input must contain hourly profile inputs for each zone."));
        }

        var duplicateProfileZoneIds = zoneHourlyProfiles
            .Select(profile => profile.ZoneId?.Trim() ?? string.Empty)
            .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .GroupBy(zoneId => zoneId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var duplicateProfileZoneId in duplicateProfileZoneIds)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.DuplicateZoneHourlyProfile",
                $"Zone hourly profile for zone '{duplicateProfileZoneId}' is duplicated."));
        }

        foreach (var zone in zones)
        {
            if (string.IsNullOrWhiteSpace(zone.ZoneId))
                continue;

            if (!zoneHourlyProfiles.Any(profile => string.Equals(profile.ZoneId, zone.ZoneId, StringComparison.OrdinalIgnoreCase)))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneHourlyProfileMissing",
                    $"Zone '{zone.ZoneId}' has no matching hourly profile."));
            }
        }

        foreach (var profile in zoneHourlyProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.ZoneId) || !zoneIdSet.Contains(profile.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneHourlyProfileZoneMissing",
                    $"Zone hourly profile references zone '{profile.ZoneId}' that is not part of this graph input."));
            }

            if (profile.ThermalCapacityJPerK < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.ZoneHourlyProfileCapacityNegative",
                    $"Zone hourly profile '{profile.ZoneId}' thermal capacity cannot be negative."));
            }

            ValidateTemperatureProfileLength(
                profile.HeatingSetpointProfileCelsius,
                $"zone '{profile.ZoneId}' heating setpoint profile",
                diagnostics);
            ValidateTemperatureProfileLength(
                profile.CoolingSetpointProfileCelsius,
                $"zone '{profile.ZoneId}' cooling setpoint profile",
                diagnostics);
            ValidateTemperatureProfileLength(
                profile.InternalGainsProfileW,
                $"zone '{profile.ZoneId}' internal gains profile",
                diagnostics);
            ValidateTemperatureProfileLength(
                profile.SolarGainsProfileW,
                $"zone '{profile.ZoneId}' solar gains profile",
                diagnostics);
            ValidateTemperatureProfileLength(
                profile.VentilationInfiltrationConductanceProfileWPerK,
                $"zone '{profile.ZoneId}' ventilation/infiltration conductance profile",
                diagnostics);
        }

        foreach (var link in boundaryLinks)
        {
            if (string.IsNullOrWhiteSpace(link.SourceZoneId) || !zoneIdSet.Contains(link.SourceZoneId))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.BoundarySourceZoneMissing",
                    $"Boundary link '{link.LinkId}' references source zone '{link.SourceZoneId}' that is not part of this graph input."));
            }

            if (link.AreaSquareMeters < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.BoundaryAreaNegative",
                    $"Boundary link '{link.LinkId}' area cannot be negative."));
            }

            if (link.ConductanceWPerK < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.BoundaryConductanceNegative",
                    $"Boundary link '{link.LinkId}' conductance cannot be negative."));
            }

            if (link.BoundaryType == MultiZoneBoundaryLinkType.InterZoneBoundary)
            {
                if (string.IsNullOrWhiteSpace(link.TargetZoneId) || !zoneIdSet.Contains(link.TargetZoneId))
                {
                    diagnostics.Add(CreateError(
                        "Iso52016.MultiZone.InputValidator.InterZoneBoundaryTargetZoneMissing",
                        $"Inter-zone boundary link '{link.LinkId}' references target zone '{link.TargetZoneId}' that is not part of this graph input."));
                }

                if (string.Equals(link.SourceZoneId, link.TargetZoneId, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(CreateError(
                        "Iso52016.MultiZone.InputValidator.InterZoneBoundarySelfLink",
                        $"Inter-zone boundary link '{link.LinkId}' cannot reference the same zone on both sides."));
                }
            }

            if (link.BoundaryType == MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone &&
                link.AdjacentBoundaryCondition is { IsAdiabaticEquivalent: false })
            {
                diagnostics.Add(CreateWarning(
                    "Iso52016.MultiZone.InputValidator.SameUseBoundaryAdiabaticHintMissing",
                    $"Same-use adjacent boundary link '{link.LinkId}' should typically set IsAdiabaticEquivalent=true for adiabatic-style foundation behavior."));
            }

            if (link.AdjacentBoundaryCondition?.TemperatureProfileCelsius is { } adjacentTemperatureProfile)
            {
                ValidateTemperatureProfileLength(
                    adjacentTemperatureProfile,
                    $"boundary link '{link.LinkId}' adjacent boundary condition",
                    diagnostics);
            }
        }

        foreach (var link in interZoneConductanceLinks)
        {
            ValidateInterZoneLink(
                linkId: link.LinkId,
                fromZoneId: link.FromZoneId,
                toZoneId: link.ToZoneId,
                zoneIdSet,
                diagnostics,
                selfLinkCode: "Iso52016.MultiZone.InputValidator.InterZoneConductanceSelfLink",
                missingZoneCode: "Iso52016.MultiZone.InputValidator.InterZoneConductanceZoneMissing",
                linkKindLabel: "inter-zone conductance link");

            if (link.AreaSquareMeters < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.InterZoneConductanceAreaNegative",
                    $"Inter-zone conductance link '{link.LinkId}' area cannot be negative."));
            }

            if (link.ConductanceWPerK < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.InterZoneConductanceNegative",
                    $"Inter-zone conductance link '{link.LinkId}' conductance cannot be negative."));
            }
        }

        foreach (var link in interZoneAirflowLinks)
        {
            ValidateInterZoneLink(
                linkId: link.LinkId,
                fromZoneId: link.FromZoneId,
                toZoneId: link.ToZoneId,
                zoneIdSet,
                diagnostics,
                selfLinkCode: "Iso52016.MultiZone.InputValidator.InterZoneAirflowSelfLink",
                missingZoneCode: "Iso52016.MultiZone.InputValidator.InterZoneAirflowZoneMissing",
                linkKindLabel: "inter-zone airflow link");

            if (link.AirflowKilogramsPerSecond < 0.0)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.InterZoneAirflowNegative",
                    $"Inter-zone airflow link '{link.LinkId}' airflow cannot be negative."));
            }
        }

        foreach (var hourlyBoundaryCondition in hourlyBoundaryConditions)
        {
            ValidateTemperatureProfileLength(
                hourlyBoundaryCondition.TemperatureProfileCelsius,
                $"hourly boundary condition '{hourlyBoundaryCondition.BoundaryId}'",
                diagnostics);
        }

        foreach (var claimFlag in claimFlags.Where(flag => !string.IsNullOrWhiteSpace(flag)))
        {
            if (!SupportedClaimFlags.Contains(claimFlag.Trim()))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.InputValidator.UnsupportedClaimFlag",
                    $"Unsupported claim flag '{claimFlag}'. This foundation supports only validation-anchor/internal-engineering-anchor standard-based claim flags."));
            }
        }

        return new MultiZoneInputValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateInterZoneLink(
        string linkId,
        string fromZoneId,
        string toZoneId,
        ISet<string> zoneIdSet,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        string selfLinkCode,
        string missingZoneCode,
        string linkKindLabel)
    {
        if (string.IsNullOrWhiteSpace(fromZoneId) || !zoneIdSet.Contains(fromZoneId))
        {
            diagnostics.Add(CreateError(
                missingZoneCode,
                $"{linkKindLabel} '{linkId}' references from-zone '{fromZoneId}' that is not part of this graph input."));
        }

        if (string.IsNullOrWhiteSpace(toZoneId) || !zoneIdSet.Contains(toZoneId))
        {
            diagnostics.Add(CreateError(
                missingZoneCode,
                $"{linkKindLabel} '{linkId}' references to-zone '{toZoneId}' that is not part of this graph input."));
        }

        if (string.Equals(fromZoneId, toZoneId, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(CreateError(
                selfLinkCode,
                $"{linkKindLabel} '{linkId}' cannot reference the same zone on both sides."));
        }
    }

    private static void ValidateTemperatureProfileLength(
        IReadOnlyList<double>? profile,
        string context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (profile is null || profile.Count == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.ProfileMissing",
                $"Profile for {context} is required and cannot be empty."));

            return;
        }

        if (profile.Count is not (1 or 8760))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.InputValidator.ProfileLengthUnsupported",
                $"Profile length for {context} must be either 1 or 8760 values."));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneInputValidator",
            Source: "Iso52016MultiZoneInputValidator",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.Foundation);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Warning,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneInputValidator",
            Source: "Iso52016MultiZoneInputValidator",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.Foundation);
}
