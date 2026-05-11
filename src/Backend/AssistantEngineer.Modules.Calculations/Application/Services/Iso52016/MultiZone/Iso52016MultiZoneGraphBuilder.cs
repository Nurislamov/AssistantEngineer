using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneGraphBuilder : ISo52016MultiZoneGraphBuilder
{
    private readonly ISo52016MultiZoneInputValidator _validator;

    public Iso52016MultiZoneGraphBuilder(
        ISo52016MultiZoneInputValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public MultiZoneCalculationResult BuildGraph(
        MultiZoneCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        var validation = _validator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var zones = input.Zones ?? [];
        var boundaryLinks = input.BoundaryLinks ?? [];
        var interZoneConductanceLinks = input.InterZoneConductanceLinks ?? [];
        var interZoneAirflowLinks = input.InterZoneAirflowLinks ?? [];

        var boundaryIdsByZone = zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ZoneId))
            .GroupBy(zone => zone.ZoneId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)(group.First().BoundaryIds ?? []),
                StringComparer.OrdinalIgnoreCase);

        foreach (var boundaryLink in boundaryLinks)
        {
            if (!boundaryIdsByZone.TryGetValue(boundaryLink.SourceZoneId, out var zoneBoundaryIds))
                continue;

            if (!zoneBoundaryIds.Contains(boundaryLink.SourceBoundaryId, StringComparer.OrdinalIgnoreCase))
            {
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.GraphBuilder.OrphanBoundaryReference",
                    $"Boundary link '{boundaryLink.LinkId}' references source boundary '{boundaryLink.SourceBoundaryId}' that is not declared by zone '{boundaryLink.SourceZoneId}'."));
            }
        }

        foreach (var interZoneLink in interZoneConductanceLinks)
        {
            ValidateZoneBoundaryReference(
                interZoneLink.LinkId,
                interZoneLink.FromZoneId,
                interZoneLink.FromBoundaryId,
                boundaryIdsByZone,
                diagnostics);

            ValidateZoneBoundaryReference(
                interZoneLink.LinkId,
                interZoneLink.ToZoneId,
                interZoneLink.ToBoundaryId,
                boundaryIdsByZone,
                diagnostics);
        }

        diagnostics.Add(CreateInfo(
            "Iso52016.MultiZone.GraphBuilder.FoundationOnly",
            "Multi-zone graph foundation assembled. Coupled hourly solving is executed separately by the multi-zone simulation service."));

        var annualSummary = new MultiZoneAnnualSummary(
            AnnualHeatingEnergyByZoneKWh: CreateZeroAnnualEnergyByZone(zones),
            AnnualCoolingEnergyByZoneKWh: CreateZeroAnnualEnergyByZone(zones));

        return new MultiZoneCalculationResult(
            BuildingId: input.BuildingId,
            Zones: zones,
            BoundaryLinks: boundaryLinks,
            InterZoneConductanceLinks: interZoneConductanceLinks,
            InterZoneAirflowLinks: interZoneAirflowLinks,
            HourlyResults: [],
            AnnualSummary: annualSummary,
            Diagnostics: diagnostics);
    }

    private static Dictionary<string, double> CreateZeroAnnualEnergyByZone(
        IReadOnlyList<ThermalZoneNode> zones) =>
        zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ZoneId))
            .GroupBy(zone => zone.ZoneId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                _ => 0.0,
                StringComparer.OrdinalIgnoreCase);

    private static void ValidateZoneBoundaryReference(
        string linkId,
        string zoneId,
        string? boundaryId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> boundaryIdsByZone,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(boundaryId))
            return;

        if (!boundaryIdsByZone.TryGetValue(zoneId, out var zoneBoundaryIds))
            return;

        if (!zoneBoundaryIds.Contains(boundaryId, StringComparer.OrdinalIgnoreCase))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.GraphBuilder.OrphanBoundaryReference",
                $"Inter-zone link '{linkId}' references boundary '{boundaryId}' that is not declared by zone '{zoneId}'."));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneGraphBuilder",
            Source: "Iso52016MultiZoneGraphBuilder",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.Foundation);

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Info,
            Code: code,
            Message: message,
            Context: "Iso52016MultiZoneGraphBuilder",
            Source: "Iso52016MultiZoneGraphBuilder",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.Foundation);
}
