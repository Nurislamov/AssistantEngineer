using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportGenerationRequest(
    EngineeringReportKind ReportKind,
    EngineeringReportFormat RequestedFormat,
    string? ReportTitle = null,
    string? ProjectId = null,
    string? BuildingId = null,
    BuildingEnergyBalanceResult? HeatingCoolingSummary = null,
    MultiZoneAnnualSummary? MultiZoneSummary = null,
    NaturalVentilationCalculationResult? NaturalVentilationSummary = null,
    BuildingGroundBoundaryCalculationResult? GroundSummary = null,
    DomesticHotWaterSystemLoadResult? DomesticHotWaterSummary = null,
    SystemEnergyCalculationSummary? SystemEnergySummary = null,
    IReadOnlyList<CalculationDiagnostic>? ValidationDiagnostics = null,
    IReadOnlyList<StandardCalculationDiagnostic>? StandardDiagnostics = null,
    CalculationTraceDocument? CalculationTrace = null,
    EngineeringReportDetailLevel DetailLevel = EngineeringReportDetailLevel.Standard,
    bool IncludeTraceAppendix = true,
    bool IncludeLimitations = true,
    DateTimeOffset? DeterministicTimestampUtc = null,
    IReadOnlyList<string>? Assumptions = null,
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<string>? SourceCalculationIds = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

