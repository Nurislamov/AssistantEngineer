using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IEngineeringReportJsonExporter
{
    string Export(
        EngineeringReportDocument report,
        bool indented = true);
}

