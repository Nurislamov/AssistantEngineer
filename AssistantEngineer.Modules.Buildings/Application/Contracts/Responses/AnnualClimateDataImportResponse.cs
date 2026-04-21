namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public class AnnualClimateDataImportResponse
{
    public int ClimateZoneId { get; set; }
    public int Year { get; set; }
    public int HourlyRecordsImported { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public IReadOnlyList<string> ImportedFields { get; set; } = [];
}
