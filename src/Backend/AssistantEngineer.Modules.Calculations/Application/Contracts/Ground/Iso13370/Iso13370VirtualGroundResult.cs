namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370VirtualGroundResult(
    double CharacteristicFloorDimensionM,
    double EquivalentGroundThicknessM,
    IReadOnlyList<double> MonthlyVirtualGroundTemperatureC,
    IReadOnlyList<double> MonthlyEquivalentGroundHeatTransferCoefficientWPerK,
    IReadOnlyList<MonthlyGroundBoundaryCondition> MonthlyBoundaryConditions,
    IReadOnlyList<double> HourlyVirtualGroundTemperatureC,
    double AnnualMeanVirtualGroundTemperatureC,
    double AnnualEquivalentGroundHeatTransferCoefficientWPerK,
    IReadOnlyList<Iso13370GroundBoundaryDiagnostics> Diagnostics);

