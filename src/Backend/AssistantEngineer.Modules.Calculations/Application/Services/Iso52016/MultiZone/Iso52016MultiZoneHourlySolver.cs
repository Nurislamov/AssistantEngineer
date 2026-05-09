using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneHourlySolver : IIso52016MultiZoneHourlySolver
{
    private const double SecondsPerHour = 3600.0;
    private const double MinimumPivot = 1e-9;

    public MultiZoneCalculationResult Solve(
        MultiZoneCalculationInput input,
        MultiZoneCalculationResult graphResult)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(graphResult);

        var diagnostics = new List<StandardCalculationDiagnostic>(graphResult.Diagnostics);
        var warningKeys = new HashSet<string>(StringComparer.Ordinal);

        var zoneProfiles = (input.ZoneHourlyProfiles ?? [])
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ZoneId))
            .GroupBy(profile => profile.ZoneId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var zones = graphResult.Zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ZoneId))
            .OrderBy(zone => zone.ZoneId, StringComparer.Ordinal)
            .ToArray();

        if (zones.Length == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.HourlySolver.NoZones",
                "Coupled multi-zone hourly solver requires at least one zone node."));

            return graphResult with { Diagnostics = diagnostics };
        }

        var zoneIndexById = zones
            .Select((zone, index) => new { zone.ZoneId, Index = index })
            .ToDictionary(item => item.ZoneId, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var hourCount = ResolveHourCount(input, zoneProfiles);
        if (hourCount is not (1 or 8760))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.HourlySolver.UnsupportedHourCount",
                $"Coupled multi-zone hourly solver requires 1 or 8760 hours, but resolved {hourCount}."));

            return graphResult with { Diagnostics = diagnostics };
        }

        var boundaryConditionsById = (input.HourlyBoundaryConditions ?? [])
            .Where(condition => !string.IsNullOrWhiteSpace(condition.BoundaryId))
            .GroupBy(condition => condition.BoundaryId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var previousTemperatures = new double[zones.Length];
        for (var i = 0; i < zones.Length; i++)
        {
            if (!zoneProfiles.TryGetValue(zones[i].ZoneId, out var profile))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.HourlySolver.ZoneProfileMissing",
                    $"Zone '{zones[i].ZoneId}' has no hourly profile for coupled multi-zone solving."));
                previousTemperatures[i] = 20.0;
                continue;
            }

            previousTemperatures[i] = profile.InitialTemperatureCelsius;
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error))
            return graphResult with { Diagnostics = diagnostics };

        var boundaryLinksByZone = graphResult.BoundaryLinks
            .Where(link => !string.IsNullOrWhiteSpace(link.SourceZoneId))
            .GroupBy(link => link.SourceZoneId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var interZoneLinks = BuildInterZoneCouplingLinks(
            graphResult.BoundaryLinks,
            graphResult.InterZoneConductanceLinks,
            zoneIndexById);

        var hourlyResults = new List<MultiZoneHourlyResult>(hourCount);

        for (var hourOfYear = 0; hourOfYear < hourCount; hourOfYear++)
        {
            var aMatrix = new double[zones.Length, zones.Length];
            var rhs = new double[zones.Length];
            var heatingSetpoints = new double[zones.Length];
            var coolingSetpoints = new double[zones.Length];

            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                var profile = zoneProfiles[zone.ZoneId];

                var heatSetpoint = ResolveProfileValue(profile.HeatingSetpointProfileCelsius, hourOfYear);
                var coolSetpoint = ResolveProfileValue(profile.CoolingSetpointProfileCelsius, hourOfYear);
                var internalGains = ResolveProfileValue(profile.InternalGainsProfileW, hourOfYear);
                var solarGains = ResolveProfileValue(profile.SolarGainsProfileW, hourOfYear);
                var ventilationConductance = Math.Max(0.0, ResolveProfileValue(profile.VentilationInfiltrationConductanceProfileWPerK, hourOfYear));

                heatingSetpoints[i] = heatSetpoint;
                coolingSetpoints[i] = coolSetpoint;

                var capacitanceTerm = Math.Max(0.0, profile.ThermalCapacityJPerK) / SecondsPerHour;
                aMatrix[i, i] += capacitanceTerm;
                rhs[i] += capacitanceTerm * previousTemperatures[i];
                rhs[i] += internalGains + solarGains;

                var hasExternalTemperature = TryResolveWeightedExternalTemperature(
                    zone.ZoneId,
                    hourOfYear,
                    boundaryLinksByZone,
                    boundaryConditionsById,
                    out var externalTemperatureCelsius);

                if (ventilationConductance > 0.0)
                {
                    if (!hasExternalTemperature)
                    {
                        AddWarningOnce(
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
                freeFloatingTemperatures = SolveLinearSystem(aMatrix, rhs);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.HourlySolver.MatrixSolveFailed",
                    $"Matrix solve failed at hour {hourOfYear}: {exception.Message}"));

                return graphResult with { Diagnostics = diagnostics };
            }

            double[] controlledTemperatures;
            double[] hvacLoadsW;
            try
            {
                (controlledTemperatures, hvacLoadsW) = ApplyHvacControl(
                    aMatrix,
                    freeFloatingTemperatures,
                    heatingSetpoints,
                    coolingSetpoints);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.HourlySolver.HvacControlFailed",
                    $"HVAC control solve failed at hour {hourOfYear}: {exception.Message}"));

                return graphResult with { Diagnostics = diagnostics };
            }

            var temperaturesByZone = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var heatingByZoneW = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var coolingByZoneW = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var buildingHeatingW = 0.0;
            var buildingCoolingW = 0.0;

            for (var i = 0; i < zones.Length; i++)
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

            hourlyResults.Add(new MultiZoneHourlyResult(
                HourOfYear: hourOfYear,
                ZoneTemperaturesCelsius: temperaturesByZone,
                HeatingLoadsByZoneW: heatingByZoneW,
                CoolingLoadsByZoneW: coolingByZoneW,
                BuildingHeatingLoadW: buildingHeatingW,
                BuildingCoolingLoadW: buildingCoolingW));

            previousTemperatures = controlledTemperatures;
        }

        var annualHeatingByZoneKWh = zones
            .ToDictionary(
                zone => zone.ZoneId,
                zone => hourlyResults.Sum(hour => hour.HeatingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                StringComparer.OrdinalIgnoreCase);
        var annualCoolingByZoneKWh = zones
            .ToDictionary(
                zone => zone.ZoneId,
                zone => hourlyResults.Sum(hour => hour.CoolingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                StringComparer.OrdinalIgnoreCase);

        var monthlySummaries = BuildMonthlySummaries(hourlyResults, zones);
        var annualSummary = new MultiZoneAnnualSummary(
            AnnualHeatingEnergyByZoneKWh: annualHeatingByZoneKWh,
            AnnualCoolingEnergyByZoneKWh: annualCoolingByZoneKWh);

        diagnostics.Add(CreateInfo(
            "Iso52016.MultiZone.HourlySolver.Completed",
            "Standard-based multi-zone calculation completed as an internal engineering anchor and validation anchor stage."));

        return graphResult with
        {
            HourlyResults = hourlyResults,
            AnnualSummary = annualSummary,
            Diagnostics = diagnostics,
            MonthlySummaries = monthlySummaries
        };
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
        if (conductance <= 0.0)
            return;

        switch (boundaryLink.BoundaryType)
        {
            case MultiZoneBoundaryLinkType.ExternalBoundary:
                {
                    if (!TryResolveBoundaryTemperature(boundaryLink.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var boundaryTemperature))
                    {
                        AddWarningOnce(
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

            case MultiZoneBoundaryLinkType.AdjacentUnconditionedZone:
                {
                    var hasTemperature = false;
                    var boundaryTemperature = 0.0;

                    if (boundaryLink.AdjacentBoundaryCondition?.TemperatureProfileCelsius is { Count: > 0 } adjacentProfile)
                    {
                        boundaryTemperature = ResolveProfileValue(adjacentProfile, hourOfYear);
                        hasTemperature = true;
                    }
                    else if (TryResolveBoundaryTemperature(boundaryLink.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var fallbackBoundaryTemperature))
                    {
                        boundaryTemperature = fallbackBoundaryTemperature;
                        hasTemperature = true;
                    }

                    if (!hasTemperature)
                    {
                        AddWarningOnce(
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
                        return;
                    }

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
        }
    }

    private static bool TryResolveWeightedExternalTemperature(
        string zoneId,
        int hourOfYear,
        IReadOnlyDictionary<string, ThermalZoneBoundaryLink[]> boundaryLinksByZone,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        out double temperatureCelsius)
    {
        temperatureCelsius = 0.0;

        if (!boundaryLinksByZone.TryGetValue(zoneId, out var links))
            return false;

        var weightedSum = 0.0;
        var totalConductance = 0.0;
        foreach (var link in links.Where(link => link.BoundaryType == MultiZoneBoundaryLinkType.ExternalBoundary))
        {
            if (!TryResolveBoundaryTemperature(link.SourceBoundaryId, hourOfYear, boundaryConditionsById, out var boundaryTemperature))
                continue;

            var conductance = Math.Max(0.0, link.ConductanceWPerK);
            weightedSum += conductance * boundaryTemperature;
            totalConductance += conductance;
        }

        if (totalConductance <= 0.0)
            return false;

        temperatureCelsius = weightedSum / totalConductance;
        return true;
    }

    private static IReadOnlyList<MultiZoneMonthlySummary> BuildMonthlySummaries(
        IReadOnlyList<MultiZoneHourlyResult> hourlyResults,
        IReadOnlyList<ThermalZoneNode> zones)
    {
        return hourlyResults
            .GroupBy(hour => ResolveMonth(hour.HourOfYear, hourlyResults.Count))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var heatingByZone = zones.ToDictionary(
                    zone => zone.ZoneId,
                    zone => group.Sum(hour => hour.HeatingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                    StringComparer.OrdinalIgnoreCase);
                var coolingByZone = zones.ToDictionary(
                    zone => zone.ZoneId,
                    zone => group.Sum(hour => hour.CoolingLoadsByZoneW[zone.ZoneId]) / 1000.0,
                    StringComparer.OrdinalIgnoreCase);

                return new MultiZoneMonthlySummary(
                    Month: group.Key,
                    HeatingEnergyByZoneKWh: heatingByZone,
                    CoolingEnergyByZoneKWh: coolingByZone,
                    BuildingHeatingEnergyKWh: group.Sum(hour => hour.BuildingHeatingLoadW) / 1000.0,
                    BuildingCoolingEnergyKWh: group.Sum(hour => hour.BuildingCoolingLoadW) / 1000.0);
            })
            .ToArray();
    }

    private static bool TryResolveBoundaryTemperature(
        string boundaryId,
        int hourOfYear,
        IReadOnlyDictionary<string, MultiZoneHourlyBoundaryCondition> boundaryConditionsById,
        out double temperatureCelsius)
    {
        temperatureCelsius = 0.0;
        if (string.IsNullOrWhiteSpace(boundaryId))
            return false;

        if (!boundaryConditionsById.TryGetValue(boundaryId, out var boundaryCondition))
            return false;

        temperatureCelsius = ResolveProfileValue(boundaryCondition.TemperatureProfileCelsius, hourOfYear);
        return true;
    }

    private static int ResolveHourCount(
        MultiZoneCalculationInput input,
        IReadOnlyDictionary<string, MultiZoneZoneHourlyProfile> zoneProfiles)
    {
        var counts = new List<int>();
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.HeatingSetpointProfileCelsius.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.CoolingSetpointProfileCelsius.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.InternalGainsProfileW.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.SolarGainsProfileW.Count));
        counts.AddRange(zoneProfiles.Values.Select(profile => profile.VentilationInfiltrationConductanceProfileWPerK.Count));
        counts.AddRange((input.HourlyBoundaryConditions ?? []).Select(condition => condition.TemperatureProfileCelsius.Count));
        counts.AddRange(input.BoundaryLinks
            .Where(link => link.AdjacentBoundaryCondition?.TemperatureProfileCelsius is not null)
            .Select(link => link.AdjacentBoundaryCondition!.TemperatureProfileCelsius.Count));

        if (counts.Count == 0)
            return 1;

        return counts.Max();
    }

    private static double ResolveProfileValue(
        IReadOnlyList<double> profile,
        int hourOfYear)
    {
        if (profile.Count == 0)
            return 0.0;

        if (profile.Count == 1)
            return profile[0];

        if (hourOfYear >= 0 && hourOfYear < profile.Count)
            return profile[hourOfYear];

        return profile[^1];
    }

    private static (double[] controlledTemperatures, double[] hvacLoadsW) ApplyHvacControl(
        double[,] aMatrix,
        double[] freeFloatingTemperatures,
        double[] heatingSetpoints,
        double[] coolingSetpoints)
    {
        var zoneCount = freeFloatingTemperatures.Length;
        var responseMatrix = BuildResponseMatrix(aMatrix);
        var controlModes = new ControlMode[zoneCount];
        var hvacLoads = new double[zoneCount];
        var controlled = (double[])freeFloatingTemperatures.Clone();

        for (var i = 0; i < zoneCount; i++)
        {
            if (freeFloatingTemperatures[i] < heatingSetpoints[i])
                controlModes[i] = ControlMode.Heating;
            else if (freeFloatingTemperatures[i] > coolingSetpoints[i])
                controlModes[i] = ControlMode.Cooling;
        }

        for (var iteration = 0; iteration < zoneCount * 4 + 8; iteration++)
        {
            var activeIndices = Enumerable.Range(0, zoneCount)
                .Where(index => controlModes[index] != ControlMode.None)
                .ToArray();

            if (activeIndices.Length == 0)
                return (controlled, hvacLoads);

            var subMatrix = new double[activeIndices.Length, activeIndices.Length];
            var rhs = new double[activeIndices.Length];

            for (var row = 0; row < activeIndices.Length; row++)
            {
                var zoneIndex = activeIndices[row];
                var target = controlModes[zoneIndex] == ControlMode.Heating
                    ? heatingSetpoints[zoneIndex]
                    : coolingSetpoints[zoneIndex];
                rhs[row] = target - freeFloatingTemperatures[zoneIndex];

                for (var column = 0; column < activeIndices.Length; column++)
                {
                    subMatrix[row, column] = responseMatrix[zoneIndex, activeIndices[column]];
                }
            }

            var activeLoads = SolveLinearSystem(subMatrix, rhs);
            Array.Clear(hvacLoads, 0, hvacLoads.Length);

            for (var i = 0; i < activeIndices.Length; i++)
            {
                hvacLoads[activeIndices[i]] = activeLoads[i];
            }

            controlled = ApplyResponse(freeFloatingTemperatures, responseMatrix, hvacLoads);

            var changed = false;
            for (var i = 0; i < activeIndices.Length; i++)
            {
                var zoneIndex = activeIndices[i];
                if (controlModes[zoneIndex] == ControlMode.Heating && hvacLoads[zoneIndex] < 0.0)
                {
                    controlModes[zoneIndex] = ControlMode.None;
                    changed = true;
                }
                else if (controlModes[zoneIndex] == ControlMode.Cooling && hvacLoads[zoneIndex] > 0.0)
                {
                    controlModes[zoneIndex] = ControlMode.None;
                    changed = true;
                }
            }

            if (changed)
                continue;

            for (var zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
            {
                if (controlModes[zoneIndex] != ControlMode.None)
                    continue;

                if (controlled[zoneIndex] < heatingSetpoints[zoneIndex])
                {
                    controlModes[zoneIndex] = ControlMode.Heating;
                    changed = true;
                }
                else if (controlled[zoneIndex] > coolingSetpoints[zoneIndex])
                {
                    controlModes[zoneIndex] = ControlMode.Cooling;
                    changed = true;
                }
            }

            if (!changed)
                return (controlled, hvacLoads);
        }

        return (controlled, hvacLoads);
    }

    private static double[] ApplyResponse(
        IReadOnlyList<double> freeFloatingTemperatures,
        double[,] responseMatrix,
        IReadOnlyList<double> hvacLoads)
    {
        var controlled = new double[freeFloatingTemperatures.Count];
        for (var i = 0; i < freeFloatingTemperatures.Count; i++)
        {
            var value = freeFloatingTemperatures[i];
            for (var j = 0; j < hvacLoads.Count; j++)
            {
                value += responseMatrix[i, j] * hvacLoads[j];
            }

            controlled[i] = value;
        }

        return controlled;
    }

    private static double[,] BuildResponseMatrix(double[,] aMatrix)
    {
        var size = aMatrix.GetLength(0);
        var response = new double[size, size];

        for (var column = 0; column < size; column++)
        {
            var rhs = new double[size];
            rhs[column] = 1.0;
            var solution = SolveLinearSystem(aMatrix, rhs);
            for (var row = 0; row < size; row++)
            {
                response[row, column] = solution[row];
            }
        }

        return response;
    }

    private static double[] SolveLinearSystem(
        double[,] matrix,
        double[] rhs)
    {
        var size = rhs.Length;
        var a = (double[,])matrix.Clone();
        var b = (double[])rhs.Clone();

        for (var pivot = 0; pivot < size; pivot++)
        {
            var bestRow = pivot;
            var bestAbs = Math.Abs(a[pivot, pivot]);

            for (var row = pivot + 1; row < size; row++)
            {
                var candidateAbs = Math.Abs(a[row, pivot]);
                if (candidateAbs > bestAbs)
                {
                    bestAbs = candidateAbs;
                    bestRow = row;
                }
            }

            if (bestAbs <= MinimumPivot)
                throw new InvalidOperationException("Matrix is singular or ill-conditioned for this multi-zone hourly step.");

            if (bestRow != pivot)
            {
                for (var col = pivot; col < size; col++)
                {
                    (a[pivot, col], a[bestRow, col]) = (a[bestRow, col], a[pivot, col]);
                }

                (b[pivot], b[bestRow]) = (b[bestRow], b[pivot]);
            }

            for (var row = pivot + 1; row < size; row++)
            {
                var factor = a[row, pivot] / a[pivot, pivot];
                if (Math.Abs(factor) <= MinimumPivot)
                    continue;

                for (var col = pivot; col < size; col++)
                {
                    a[row, col] -= factor * a[pivot, col];
                }

                b[row] -= factor * b[pivot];
            }
        }

        var solution = new double[size];
        for (var row = size - 1; row >= 0; row--)
        {
            var sum = b[row];
            for (var col = row + 1; col < size; col++)
            {
                sum -= a[row, col] * solution[col];
            }

            solution[row] = sum / a[row, row];
        }

        return solution;
    }

    private static IReadOnlyList<CouplingLink> BuildInterZoneCouplingLinks(
        IReadOnlyList<ThermalZoneBoundaryLink> boundaryLinks,
        IReadOnlyList<InterZoneConductanceLink> interZoneConductanceLinks,
        IReadOnlyDictionary<string, int> zoneIndexById)
    {
        var links = new List<CouplingLink>();

        foreach (var boundaryLink in boundaryLinks.Where(link => link.BoundaryType == MultiZoneBoundaryLinkType.InterZoneBoundary))
        {
            if (string.IsNullOrWhiteSpace(boundaryLink.TargetZoneId))
                continue;
            if (!zoneIndexById.TryGetValue(boundaryLink.SourceZoneId, out var fromIndex))
                continue;
            if (!zoneIndexById.TryGetValue(boundaryLink.TargetZoneId, out var toIndex))
                continue;
            if (fromIndex == toIndex)
                continue;

            links.Add(new CouplingLink(fromIndex, toIndex, Math.Max(0.0, boundaryLink.ConductanceWPerK)));
        }

        foreach (var link in interZoneConductanceLinks)
        {
            if (!zoneIndexById.TryGetValue(link.FromZoneId, out var fromIndex))
                continue;
            if (!zoneIndexById.TryGetValue(link.ToZoneId, out var toIndex))
                continue;
            if (fromIndex == toIndex)
                continue;

            links.Add(new CouplingLink(fromIndex, toIndex, Math.Max(0.0, link.ConductanceWPerK)));
        }

        return links;
    }

    private static int ResolveMonth(
        int hourOfYear,
        int hourCount)
    {
        if (hourCount != 8760)
            return 1;

        var monthHours =
            new[] { 744, 672, 744, 720, 744, 720, 744, 744, 720, 744, 720, 744 };
        var cursor = 0;
        for (var month = 1; month <= monthHours.Length; month++)
        {
            cursor += monthHours[month - 1];
            if (hourOfYear < cursor)
                return month;
        }

        return 12;
    }

    private static void AddWarningOnce(
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ISet<string> warningKeys,
        string key,
        string code,
        string message)
    {
        if (!warningKeys.Add(key))
            return;

        diagnostics.Add(CreateWarning(code, message));
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Warning,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Info,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneHourlySolver",
            Source: "Iso52016MultiZoneHourlySolver",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.HeatTransfer);

    private readonly record struct CouplingLink(
        int FromIndex,
        int ToIndex,
        double ConductanceWPerK);

    private enum ControlMode
    {
        None = 0,
        Heating = 1,
        Cooling = 2
    }
}
