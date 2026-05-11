using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneHourlySolver : ISo52016MultiZoneHourlySolver
{
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
            diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateError(
                "Iso52016.MultiZone.HourlySolver.NoZones",
                "Coupled multi-zone hourly solver requires at least one zone node."));

            return graphResult with { Diagnostics = diagnostics };
        }

        var zoneIndexById = zones
            .Select((zone, index) => new { zone.ZoneId, Index = index })
            .ToDictionary(item => item.ZoneId, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var hourCount = Iso52016MultiZoneBoundaryResolver.ResolveHourCount(input, zoneProfiles);
        if (hourCount is not (1 or 8760))
        {
            diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateError(
                "Iso52016.MultiZone.HourlySolver.UnsupportedHourCount",
                $"Coupled multi-zone hourly solver requires 1 or 8760 hours, but resolved {hourCount}."));

            return graphResult with { Diagnostics = diagnostics };
        }

        var boundaryConditionsById = (input.HourlyBoundaryConditions ?? [])
            .Where(condition => !string.IsNullOrWhiteSpace(condition.BoundaryId))
            .GroupBy(condition => condition.BoundaryId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var initialTemperatures = new double[zones.Length];
        for (var i = 0; i < zones.Length; i++)
        {
            if (!zoneProfiles.TryGetValue(zones[i].ZoneId, out var profile))
            {
                diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateError(
                    "Iso52016.MultiZone.HourlySolver.ZoneProfileMissing",
                    $"Zone '{zones[i].ZoneId}' has no hourly profile for coupled multi-zone solving."));
                initialTemperatures[i] = 20.0;
                continue;
            }

            initialTemperatures[i] = profile.InitialTemperatureCelsius;
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error))
            return graphResult with { Diagnostics = diagnostics };

        var boundaryLinksByZone = graphResult.BoundaryLinks
            .Where(link => !string.IsNullOrWhiteSpace(link.SourceZoneId))
            .GroupBy(link => link.SourceZoneId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var interZoneLinks = Iso52016MultiZoneCouplingBuilder.BuildInterZoneCouplingLinks(
            graphResult.BoundaryLinks,
            graphResult.InterZoneConductanceLinks,
            zoneIndexById);

        if (!Iso52016MultiZoneHourlySimulationLoop.TryRun(
                hourCount,
                zones,
                zoneProfiles,
                zoneIndexById,
                boundaryLinksByZone,
                boundaryConditionsById,
                interZoneLinks,
                initialTemperatures,
                diagnostics,
                warningKeys,
                out var hourlyResults))
        {
            return graphResult with { Diagnostics = diagnostics };
        }

        var annualHeatingByZoneKWh = Iso52016MultiZoneResultAggregator.BuildAnnualHeatingByZoneKWh(zones, hourlyResults);
        var annualCoolingByZoneKWh = Iso52016MultiZoneResultAggregator.BuildAnnualCoolingByZoneKWh(zones, hourlyResults);
        var monthlySummaries = Iso52016MultiZoneResultAggregator.BuildMonthlySummaries(hourlyResults, zones);

        var annualSummary = new MultiZoneAnnualSummary(
            AnnualHeatingEnergyByZoneKWh: annualHeatingByZoneKWh,
            AnnualCoolingEnergyByZoneKWh: annualCoolingByZoneKWh);

        diagnostics.Add(Iso52016MultiZoneSolverDiagnostics.CreateInfo(
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
}
