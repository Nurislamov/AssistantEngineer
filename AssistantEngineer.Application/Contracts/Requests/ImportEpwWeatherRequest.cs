namespace AssistantEngineer.Application.Contracts.Requests;

public class ImportEpwWeatherRequest
{
    public string FilePath { get; set; } = string.Empty;
    public int Year { get; set; } = 2020;
}
