using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationHourlyInputBuilder
{
    NaturalVentilationCalculationInput BuildHourlyAirflowInput(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationHourlyZoneEnvironment environment,
        IReadOnlyList<NaturalVentilationOpeningOperationResult> operationsForHour);
}
