namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationExpectedResult(
    double? AnnualHeatingKWh = null,
    double? AnnualCoolingKWh = null,
    double? PeakHeatingW = null,
    double? PeakCoolingW = null,
    double? MeanOperativeTemperatureC = null,
    double? MaxOperativeTemperatureC = null,
    double? MinOperativeTemperatureC = null,
    int? HourlyResultCount = null,
    IReadOnlyList<double>? MonthlyHeatingKWh = null,
    IReadOnlyList<double>? MonthlyCoolingKWh = null);
