using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Facades;

public interface IReportsFacade
{
    Task<Result<BuildingReport>> BuildBuildingReportAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken);

    Task<Result<byte[]>> GenerateBuildingReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken);

    Task<Result<HeatingReport>> BuildHeatingReportAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<byte[]>> GenerateEnergyBalanceReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken);
}
