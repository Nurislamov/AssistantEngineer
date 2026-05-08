using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationOpeningControlEvaluator
{
    NaturalVentilationOpeningOperationResult Evaluate(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context);

    NaturalVentilationControlEvaluationResult Evaluate(
        NaturalVentilationControlEvaluationInput input);
}
