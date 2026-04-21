using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IBuildingReportExporter
{
    byte[] GenerateBuildingReport(BuildingReport report, CancellationToken cancellationToken = default);
    byte[] GenerateEnergyBalanceReport(BuildingEnergyBalanceResult report, CancellationToken cancellationToken = default);
}