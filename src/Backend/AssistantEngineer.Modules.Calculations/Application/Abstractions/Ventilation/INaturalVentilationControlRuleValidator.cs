using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationControlRuleValidator
{
    NaturalVentilationControlRuleValidationResult Validate(
        IReadOnlyList<NaturalVentilationOpeningControlRule> rules);
}
