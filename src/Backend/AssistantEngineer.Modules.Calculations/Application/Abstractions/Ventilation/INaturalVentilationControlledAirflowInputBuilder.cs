using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationControlledAirflowInputBuilder
{
    NaturalVentilationCalculationInput BuildHourlyAirflowInput(
        NaturalVentilationCalculationInput baseInput,
        IReadOnlyList<NaturalVentilationOpeningOperationResult> operationsForHour);
}
