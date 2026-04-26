using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;

public sealed record VentilationCalculationContext(
    VentilationCalculationMethod Method,
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double WindSpeedMPerS = 0,
    double? CustomHeatTransferCoefficientWPerK = null);