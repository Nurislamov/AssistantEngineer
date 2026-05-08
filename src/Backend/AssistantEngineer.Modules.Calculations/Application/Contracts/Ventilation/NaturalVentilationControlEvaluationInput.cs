using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationControlEvaluationInput(
    IReadOnlyList<NaturalVentilationOpeningControlRule> Rules,
    IReadOnlyList<NaturalVentilationHourlyControlContext> HourlyContexts,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
