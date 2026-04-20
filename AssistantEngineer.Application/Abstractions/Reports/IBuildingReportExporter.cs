using AssistantEngineer.Application.Contracts.Reports;

namespace AssistantEngineer.Application.Abstractions;

public interface IBuildingReportExporter
{
    byte[] GenerateBuildingReport(BuildingReport report, CancellationToken cancellationToken = default);
    byte[] GenerateEnergyBalanceReport(BuildingEnergyBalanceResult report, CancellationToken cancellationToken = default);
}
