using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterFixtureUseInput(
    string FixtureId,
    string? Name,
    double? UsesPerDay,
    double? LitersPerUse,
    double? UseDurationMinutes,
    double? FlowRateLitersPerMinute,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
