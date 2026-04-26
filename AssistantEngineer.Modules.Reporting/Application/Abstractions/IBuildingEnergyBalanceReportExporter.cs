using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IBuildingEnergyBalanceReportExporter
{
    byte[] GenerateEnergyBalanceReport(
        BuildingEnergyBalanceResult report,
        CancellationToken cancellationToken = default);
}