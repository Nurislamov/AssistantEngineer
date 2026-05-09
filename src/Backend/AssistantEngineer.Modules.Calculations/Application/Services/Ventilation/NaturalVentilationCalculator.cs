using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationCalculator : INaturalVentilationCalculator
{
    private readonly INaturalVentilationZoneLoadCalculator _zoneLoadCalculator;

    public NaturalVentilationCalculator(INaturalVentilationZoneLoadCalculator zoneLoadCalculator)
    {
        _zoneLoadCalculator = zoneLoadCalculator ?? throw new ArgumentNullException(nameof(zoneLoadCalculator));
    }

    public NaturalVentilationZoneIntegrationResult Calculate(
        NaturalVentilationZoneIntegrationInput input) =>
        _zoneLoadCalculator.Calculate(input);
}
