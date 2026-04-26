using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IBuildingCoolingReportExporter
{
    byte[] GenerateCoolingReport(
        BuildingCoolingReport report,
        CancellationToken cancellationToken = default);
}