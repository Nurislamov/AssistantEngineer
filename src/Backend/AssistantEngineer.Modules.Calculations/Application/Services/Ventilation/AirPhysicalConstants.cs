namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public static class AirPhysicalConstants
{
    public const double AirDensityKgPerM3 = 1.2;
    public const double AirSpecificHeatJPerKgK = 1005.0;

    public static double AirHeatCapacityWhPerM3K =>
        AirDensityKgPerM3 * AirSpecificHeatJPerKgK / 3600.0;
}
