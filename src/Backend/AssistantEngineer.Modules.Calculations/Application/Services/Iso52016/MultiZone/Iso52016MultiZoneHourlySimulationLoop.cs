using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneHourlySimulationLoop
{
    private const double SecondsPerHour = 3600.0;

    internal static bool TryRun(
        int hourCount,
        IReadOnlyList<ThermalZoneNode> zones,
        IReadOnlyDictionary<string, MultiZoneZoneHourlyProfile> zoneProfiles,
        IReadOnlyDictionary<string, int> zoneIndexById,
        IReadOnlyDictionary<string, ThermalZoneBoundaryLink[]> boundaryLinksByZone,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        IReadOnlyList<CouplingLink> interZoneLinks,
        double[] initialTemperatures,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ISet<string> warningKeys,
        out IReadOnlyList<MultiZoneHourlyResult> hourlyResults)
    {
        var previousTemperatures = (double[])initialTemperatures.Clone();
        var localResults = new List<MultiZoneHourlyResult>(hourCount);

        for (var hourOfYear = 0; hourOfYear < hourCount; hourOfYear++)
        {
            var aMatrix = new double[zones.Count, zones.Count];
            var rhs = new double[zones.Count];
            var heatingSetpoints = new double[zones.Count];
            var coolingSetpoints = new double[zones.Count];

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                var profile = zoneProfiles[zone.ZoneId];

                var heatSetpoint = Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(profile.HeatingSetpointProfileCelsius, hourOfYear);
                var coolSetpoint = Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(profile.CoolingSetpointProfileCelsius, hourOfYear);
                var internalGains = Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(profile.InternalGainsProfileW, hourOfYear);
                var solarGains = Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(profile.SolarGainsProfileW, hourOfYear);
                var ventilationConductance = Math.Max(0.0, Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(profile.VentilationInfiltrationConductanceProfileWPerK, hourOfYear));

                heatingSetpoints[i] = heatSetpoint;
                coolingSetpoints[i] = coolSetpoint;

                var capacitanceTerm = Math.Max(0.0, profile.ThermalCapacityJPerK) / SecondsPerHour;
                aMatrix[i, i] += capacitanceTerm;
                rhs[i] += capacitanceTerm * previousTemperatures[i];
                rhs[i] += internalGains + solarGains;

                var hasExternalTemperature = Iso52016MultiZoneBoundaryResolver.TryResolveWeightedExternalTemperature(
                    zone.ZoneId,
                    hourOfYear,
                    boundaryLinksByZone,
                    boundaryConditionsById,
                    out var externalTemperatureCelsius);

                if (ventilationConductance > 0.0)
                {
                    if (!hasExternalTemperature)
                    {
                        Iso52016MultiZoneSolverDiagnostics.AddWarningOnce(
                            diagnostics,
                            warningKeys,
                            key: $"NO-OUTDOOR-{zone.ZoneId}",
                            code: "Iso52016.MultiZone.HourlySolver.OutdoorTemperatureMissingForVentilation",
                            message: $"Zone '{zone.ZoneId}' has ventilation/infiltration conductance but no external boundary temperature. Ventilation transfer is skipped.");
                    }
                    else
                    {
                        aMatrix[i, i] += ventilationConductance;
                        rhs[i] += ventilationConductance * externalTemperatureCelsius;
                    }
                }

                if (boundaryLinksByZone.TryGetValue(zone.ZoneId, out var zoneBoundaryLinks))
                {
                    foreach (var boundaryLink in zoneBoundaryLinks)
                    {
                        AddBoundaryContribution(
                            boundaryLink,
                            zone.ZoneId,
                            zoneIndexById,
                            hourOfYear,
                            boundaryConditionsById,
                            aMatrix,
                            rhs,
                            i,
                            diagnostics,
                            warningKeys);
                    }
                }
            }

            foreach (var interZoneLink in interZoneLinks)
            {
                if (interZoneLink.ConductanceWPerK <= 0.0)
                    continue;

                aMatrix[interZoneLink.FromIndex, interZoneLink.FromIndex] += interZoneLink.ConductanceWPerK;
                aMatrix[interZoneLink.ToIndex, interZoneLink.ToIndex] += interZoneLink.ConductanceWPerK;
                aMatrix[interZoneLink.FromIndex, interZoneLink.ToIndex] -= interZoneLink.ConductanceWPerK;
                aMatrix[interZoneLink.ToIndex, interZoneLink.FromIndex] -= interZoneLink.ConductanceWPerK;
            }

            double[] freeFloatingTemperatures;
            try
            {
                freeFloatingTemperatures = Iso52016MultiZoneLinearSystem.SolveLinearSystem(aMatrix, rhs);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateError(
                    "Iso52016.MultiZone.HourlySolver.MatrixSolveFailed",
                    $"Matrix solve failed at hour {hourOfYear}: {exception.Message}"));

                hourlyResults = [];
                return false;
            }

            double[] controlledTemperatures;
            double[] hvacLoadsW;
            try
            {
                (controlledTemperatures, hvacLoadsW) = Iso52016MultiZoneHvacController.ApplyHvacControl(
                    aMatrix,
                    freeFloatingTemperatures,
                    heatingSetpoints,
                    coolingSetpoints);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateError(
                    "Iso52016.MultiZone.HourlySolver.HvacControlFailed",
                    $"HVAC control solve failed at hour {hourOfYear}: {exception.Message}"));

                hourlyResults = [];
                return false;
            }

            var temperaturesByZone = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var heatingByZoneW = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var coolingByZoneW = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var buildingHeatingW = 0.0;
            var buildingCoolingW = 0.0;

            for (var i = 0; i < zones.Count; i++)
            {
                var zoneId = zones[i].ZoneId;
                var heatingW = Math.Max(0.0, hvacLoadsW[i]);
                var coolingW = Math.Max(0.0, -hvacLoadsW[i]);

                temperaturesByZone[zoneId] = controlledTemperatures[i];
                heatingByZoneW[zoneId] = heatingW;
                coolingByZoneW[zoneId] = coolingW;

                buildingHeatingW += heatingW;
                buildingCoolingW += coolingW;
            }

            localResults.Add(new MultiZoneHourlyResult(
                HourOfYear: hourOfYear,
                ZoneTemperaturesCelsius: temperaturesByZone,
                HeatingLoadsByZoneW: heatingByZoneW,
                CoolingLoadsByZoneW: coolingByZoneW,
                BuildingHeatingLoadW: buildingHeatingW,
                BuildingCoolingLoadW: buildingCoolingW));

            previousTemperatures = controlledTemperatures;
        }

        hourlyResults = localResults;
        return true;
    }

    private static void AddBoundaryContribution(
        ThermalZoneBoundaryLink boundaryLink,
        string zoneId,
        IReadOnlyDictionary<string, int> zoneIndexById,
        int hourOfYear,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        double[,] aMatrix,
        double[] rhs,
        int zoneIndex,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ISet<string> warningKeys)
    {
        var conductance = Math.Max(0.0, boundaryLink.ConductanceWPerK);

        switch (boundaryLink.BoundaryType)
        {
            case MultiZoneBoundaryLinkType.ExternalBoundary:
                {
                    if (conductance <= 0.0)
                        return;

                    if (!Iso52016MultiZoneBoundaryResolver.TryResolveBoundaryTemperature(boundaryLink.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var boundaryTemperature))
                    {
                        Iso52016MultiZoneSolverDiagnostics.AddWarningOnce(
                            diagnostics,
                            warningKeys,
                            key: $"NO-EXT-BOUNDARY-TEMP-{boundaryLink.LinkId}",
                            code: "Iso52016.MultiZone.HourlySolver.ExternalBoundaryTemperatureMissing",
                            message: $"External boundary link '{boundaryLink.LinkId}' has no temperature profile. Transfer for this boundary is skipped.");
                        return;
                    }

                    aMatrix[zoneIndex, zoneIndex] += conductance;
                    rhs[zoneIndex] += conductance * boundaryTemperature;
                    break;
                }

            case MultiZoneBoundaryLinkType.GroundBoundary:
                {
                    if (conductance <= 0.0)
                        return;

                    if (!Iso52016MultiZoneBoundaryResolver.TryResolveBoundaryTemperature(boundaryLink.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var boundaryTemperature))
                    {
                        Iso52016MultiZoneSolverDiagnostics.AddWarningOnce(
                            diagnostics,
                            warningKeys,
                            key: $"NO-GROUND-BOUNDARY-TEMP-{boundaryLink.LinkId}",
                            code: "Iso52016.MultiZone.HourlySolver.GroundBoundaryTemperatureMissing",
                            message: $"Ground boundary link '{boundaryLink.LinkId}' has no temperature profile. Transfer for this boundary is skipped.");
                        return;
                    }

                    aMatrix[zoneIndex, zoneIndex] += conductance;
                    rhs[zoneIndex] += conductance * boundaryTemperature;
                    break;
                }

            case MultiZoneBoundaryLinkType.AdjacentUnconditionedZone:
                {
                    if (conductance <= 0.0)
                        return;

                    var hasTemperature = false;
                    var boundaryTemperature = 0.0;

                    if (boundaryLink.AdjacentBoundaryCondition?.TemperatureProfileCelsius is { Count: > 0 } adjacentProfile)
                    {
                        boundaryTemperature = Iso52016MultiZoneBoundaryResolver.ResolveProfileValue(adjacentProfile, hourOfYear);
                        hasTemperature = true;
                    }
                    else if (Iso52016MultiZoneBoundaryResolver.TryResolveBoundaryTemperature(boundaryLink.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var fallbackBoundaryTemperature))
                    {
                        boundaryTemperature = fallbackBoundaryTemperature;
                        hasTemperature = true;
                    }

                    if (!hasTemperature)
                    {
                        Iso52016MultiZoneSolverDiagnostics.AddWarningOnce(
                            diagnostics,
                            warningKeys,
                            key: $"NO-ADJ-UNCOND-TEMP-{boundaryLink.LinkId}",
                            code: "Iso52016.MultiZone.HourlySolver.AdjacentUnconditionedTemperatureMissing",
                            message: $"Adjacent unconditioned boundary link '{boundaryLink.LinkId}' has no boundary temperature profile. Transfer for this boundary is skipped.");
                        return;
                    }

                    aMatrix[zoneIndex, zoneIndex] += conductance;
                    rhs[zoneIndex] += conductance * boundaryTemperature;
                    break;
                }

            case MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone:
                {
                    if (boundaryLink.AdjacentBoundaryCondition?.IsAdiabaticEquivalent == true)
                    {
                        Iso52016MultiZoneSolverDiagnostics.AddWarningOnce(
                            diagnostics,
                            warningKeys,
                            key: $"SAME-USE-ADIABATIC-{boundaryLink.LinkId}",
                            code: "Iso52016.MultiZone.HourlySolver.SameUseAdiabaticPolicyApplied",
                            message: $"Same-use adjacent boundary '{boundaryLink.LinkId}' is treated as adiabatic-style and does not add exterior loss.");
                        return;
                    }

                    if (conductance <= 0.0)
                        return;

                    if (!string.IsNullOrWhiteSpace(boundaryLink.TargetZoneId) &&
                        zoneIndexById.TryGetValue(boundaryLink.TargetZoneId, out var targetZoneIndex) &&
                        targetZoneIndex != zoneIndex)
                    {
                        aMatrix[zoneIndex, zoneIndex] += conductance;
                        aMatrix[targetZoneIndex, targetZoneIndex] += conductance;
                        aMatrix[zoneIndex, targetZoneIndex] -= conductance;
                        aMatrix[targetZoneIndex, zoneIndex] -= conductance;
                    }

                    break;
                }

            case MultiZoneBoundaryLinkType.InterZoneBoundary:
                {
                    if (conductance <= 0.0)
                        return;
                    break;
                }
        }
    }
}
