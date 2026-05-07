using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationPressureCalculator
{
    NaturalVentilationPressureResult CalculateWindPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment);

    NaturalVentilationPressureResult CalculateStackPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment);

    NaturalVentilationPressureResult CalculateCombinedPressure(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment);
}
