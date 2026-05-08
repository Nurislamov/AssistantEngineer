using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLossValidationResult(
    bool IsValid,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
