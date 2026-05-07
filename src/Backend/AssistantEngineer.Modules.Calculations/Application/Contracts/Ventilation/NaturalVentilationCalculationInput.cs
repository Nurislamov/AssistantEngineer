using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationCalculationInput(
    string CalculationId,
    NaturalVentilationFlowConfiguration FlowConfiguration,
    IReadOnlyList<NaturalVentilationOpeningGeometry> Openings,
    NaturalVentilationEnvironment Environment,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
