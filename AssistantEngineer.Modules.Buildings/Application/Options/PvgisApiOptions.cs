namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class PvgisApiOptions
{
    public const string SectionName = "Buildings:Pvgis";

    public string BaseUrl { get; set; } = "https://re.jrc.ec.europa.eu/api/";
    public int TimeoutSeconds { get; set; } = 60;
}