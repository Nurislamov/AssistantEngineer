using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationAirflowCalculator
{
    NaturalVentilationCalculationResult Calculate(NaturalVentilationCalculationInput input);
}
