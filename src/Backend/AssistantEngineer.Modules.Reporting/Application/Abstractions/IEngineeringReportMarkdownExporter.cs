using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IEngineeringReportMarkdownExporter
{
    string Export(
        EngineeringReportDocument report);
}

