namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportTable(
    string TableId,
    string Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyDictionary<string, string> Units,
    IReadOnlyList<string> Notes);

