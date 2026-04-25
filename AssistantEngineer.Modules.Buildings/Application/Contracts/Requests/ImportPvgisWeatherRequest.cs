namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class ImportPvgisWeatherRequest
{
    public int Year { get; set; } = 2020;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? StartYear { get; set; } = 2005;
    public int? EndYear { get; set; } = 2020;
    public bool UseHorizon { get; set; } = true;
    public string? RadiationDatabase { get; set; }
}