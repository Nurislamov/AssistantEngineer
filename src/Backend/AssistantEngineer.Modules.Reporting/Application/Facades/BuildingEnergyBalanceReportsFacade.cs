using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public sealed class BuildingEnergyBalanceReportsFacade : IBuildingEnergyBalanceReportsFacade
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IBuildingEnergyBalanceReportExporter _reportExporter;

    internal BuildingEnergyBalanceReportsFacade(
        ILoadCalculationsFacade loadCalculations,
        IBuildingEnergyBalanceReportExporter reportExporter)
    {
        _loadCalculations = loadCalculations;
        _reportExporter = reportExporter;
    }

    public async Task<Result<byte[]>> GenerateEnergyBalanceReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _loadCalculations.CalculateBuildingEnergyBalanceAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);

        if (result.IsFailure)
            return Result<byte[]>.Failure(result);

        return Result<byte[]>.Success(
            _reportExporter.GenerateEnergyBalanceReport(
                result.Value,
                cancellationToken));
    }
}