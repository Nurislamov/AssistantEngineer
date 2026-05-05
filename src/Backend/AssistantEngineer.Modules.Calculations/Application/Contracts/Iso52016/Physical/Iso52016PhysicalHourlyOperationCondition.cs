namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Optional hourly operation profile row for the physical room/zone model builder.
/// Values not specified here fall back to the hourly input profile or model options.
/// </summary>
public sealed record Iso52016PhysicalHourlyOperationCondition(
    int HourOfYear,
    double? VentilationHeatTransferCoefficientWPerK = null,
    double? VentilationBoundaryTemperatureC = null,
    double? InternalGainsConvectiveFraction = null,
    double? SolarGainsToAirFraction = null);
